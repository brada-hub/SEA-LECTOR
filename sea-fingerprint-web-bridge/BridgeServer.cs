using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using AxZKFPEngXControl;

namespace SeaZktecoWebBridge
{
    public class BridgeServerForm : Form
    {
        private AxZKFPEngX axZkFP;
        private HttpListener httpListener;
        private Thread httpThread;
        private bool isRunning = true;
        private readonly int port = 8080;

        private int fpcHandle = 0;
        private int currentFpid = 1;
        private string lastTemplate9 = "";
        private string lastTemplate10 = "";
        private string lastCapturedImageBase64 = "";
        private bool isConnected = false;

        // Lista de clientes SSE conectados
        private readonly List<HttpListenerResponse> sseClients = new List<HttpListenerResponse>();
        private readonly object sseLock = new object();
        private readonly JavaScriptSerializer json = new JavaScriptSerializer();

        // Controles de interfaz gráfica mínima
        private TextBox logBox;
        private Button openBrowserBtn;
        private Label statusLabel;

        [STAThread]
        public static void Main()
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bridge_debug.log");
            try { File.WriteAllText(logPath, "=== SERVICIO PUENTE ZKTECO INICIADO ===\r\nFecha: " + DateTime.Now.ToString() + "\r\n"); } catch {}

            try
            {
                Console.Title = "SEA - Puente Biometrico ZKTeco (Puerto 8080)";
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("======================================================================");
                Console.WriteLine("        SEA - PUENTE BIOMETRICO ZKTECO (PUERTO 8080)");
                Console.WriteLine("======================================================================");
                Console.ResetColor();
            }
            catch {}

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += (s, e) => {
                try { File.AppendAllText(logPath, "ThreadException: " + e.Exception.ToString() + Environment.NewLine); } catch {}
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                try { File.AppendAllText(logPath, "AppDomain: " + e.ExceptionObject.ToString() + Environment.NewLine); } catch {}
            };

            BridgeServerForm form = null;
            try
            {
                form = new BridgeServerForm();
                Application.Run(form);
            }
            catch (Exception ex)
            {
                try { File.AppendAllText(logPath, "RunException: " + ex.ToString() + Environment.NewLine); } catch {}
                try { Console.WriteLine("[AVISO] Formulario GUI no disponible: " + ex.Message); } catch {}
            }

            // Mantener el servicio HTTP en ejecución permanente en consola
            try { Console.WriteLine("Servicio HTTP activo y escuchando en http://localhost:8080/"); } catch {}
            while (true)
            {
                Thread.Sleep(10000);
            }
        }

        public BridgeServerForm()
        {
            this.Text = "SEA - ZKTeco Fingerprint Web Bridge";
            this.Size = new Size(620, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Shield;

            BuildUI();
            StartHttpServer();
            InitializeActiveX();

            this.Shown += (s, e) =>
            {
                if (driverInstalled) ConnectDevice();
            };
        }

        private void BuildUI()
        {
            var panelTop = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.FromArgb(245, 247, 250) };
            statusLabel = new Label
            {
                Text = "Iniciando servicio en http://localhost:" + port + " ...",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(15, 12),
                AutoSize = true,
                ForeColor = Color.FromArgb(40, 50, 70)
            };
            var hintLabel = new Label
            {
                Text = "Mantén esta ventana abierta mientras uses tu aplicación Web.",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(15, 34),
                AutoSize = true,
                ForeColor = Color.Gray
            };

            openBrowserBtn = new Button
            {
                Text = "Abrir Web de Prueba",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Size = new Size(160, 36),
                Location = new Point(430, 15),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            openBrowserBtn.FlatAppearance.BorderSize = 0;
            openBrowserBtn.Click += (s, e) =>
            {
                try { System.Diagnostics.Process.Start("http://localhost:" + port); } catch { }
            };

            panelTop.Controls.Add(statusLabel);
            panelTop.Controls.Add(hintLabel);
            panelTop.Controls.Add(openBrowserBtn);

            logBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 24, 33),
                ForeColor = Color.FromArgb(220, 230, 242),
                Font = new Font("Consolas", 9.5f)
            };

            this.Controls.Add(logBox);
            this.Controls.Add(panelTop);
        }

        private void Log(string msg)
        {
            string entry = string.Format("[{0:HH:mm:ss}] {1}", DateTime.Now, msg);
            try { Console.WriteLine(entry); } catch {}
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bridge_debug.log");
                File.AppendAllText(logPath, entry + Environment.NewLine);
            }
            catch {}

            if (this.InvokeRequired)
            {
                try { this.BeginInvoke(new Action<string>(Log), msg); } catch {}
                return;
            }
            if (logBox != null)
            {
                logBox.AppendText(entry + Environment.NewLine);
            }
        }

        private void SafeInvoke(Action action)
        {
            if (this.IsHandleCreated && !this.IsDisposed && this.InvokeRequired)
            {
                try { this.Invoke(action); } catch { action(); }
            }
            else
            {
                action();
            }
        }

        private bool driverInstalled = false;

        private void InitializeActiveX()
        {
            try
            {
                axZkFP = new AxZKFPEngX();
                ((System.ComponentModel.ISupportInitialize)(axZkFP)).BeginInit();
                this.Controls.Add(axZkFP);
                ((System.ComponentModel.ISupportInitialize)(axZkFP)).EndInit();

                // Forzar la creación de la instancia OCX dentro del try/catch
                axZkFP.CreateControl();

                axZkFP.OnFeatureInfo += AxZkFP_OnFeatureInfo;
                axZkFP.OnImageReceived += AxZkFP_OnImageReceived;
                axZkFP.OnEnroll += AxZkFP_OnEnroll;
                axZkFP.OnCapture += AxZkFP_OnCapture;

                driverInstalled = true;
                Log("Componente biométrico ActiveX ZKTeco inicializado correctamente.");

                // Intentar conectar al sensor físico de inmediato
                ConnectDevice();
            }
            catch (Exception ex)
            {
                driverInstalled = false;
                if (axZkFP != null)
                {
                    try { this.Controls.Remove(axZkFP); } catch {}
                    try { axZkFP.Dispose(); } catch {}
                    axZkFP = null;
                }
                isConnected = false;
                statusLabel.Text = "ℹ️ Servidor Activo (Esperando Conexión de Lector ZKTeco)";
                statusLabel.ForeColor = Color.FromArgb(202, 138, 4);
                Log("AVISO: " + ex.Message);
                Log("Servidor Web Bridge 8080 listo y escuchando peticiones.");
            }
        }

        private bool ConnectDevice()
        {
            try
            {
                if (axZkFP == null) return false;
                if (axZkFP.InitEngine() == 0)
                {
                    axZkFP.FPEngineVersion = "10";
                    fpcHandle = axZkFP.CreateFPCacheDBEx();
                    isConnected = true;
                    string sn = axZkFP.SensorSN;
                    Log("Lector ZKTeco CONECTADO. N/S: " + (string.IsNullOrEmpty(sn) ? "Desconocido" : sn));
                    statusLabel.Text = "🟢 Conectado - Lector ZKTeco listo en puerto " + port;
                    statusLabel.ForeColor = Color.FromArgb(16, 120, 50);

                    BroadcastSse(new Dictionary<string, object>
                    {
                        { "event", "device_connected" },
                        { "serial", sn },
                        { "version", "10.0" }
                    });
                    return true;
                }
                else
                {
                    isConnected = false;
                    Log("No se pudo conectar al lector (InitEngine != 0). Verifique cable USB.");
                    statusLabel.Text = "🔴 Desconectado - Verifique conexión USB del lector";
                    statusLabel.ForeColor = Color.FromArgb(180, 40, 40);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log("Error al conectar lector: " + ex.Message);
                return false;
            }
        }

        private void DisconnectDevice()
        {
            try
            {
                if (axZkFP != null && isConnected)
                {
                    axZkFP.EndEngine();
                    isConnected = false;
                    Log("Lector desconectado.");
                    statusLabel.Text = "⚪ Desconectado manualmente";
                    statusLabel.ForeColor = Color.Gray;

                    BroadcastSse(new Dictionary<string, object>
                    {
                        { "event", "device_disconnected" }
                    });
                }
            }
            catch (Exception ex)
            {
                Log("Error al desconectar: " + ex.Message);
            }
        }

        private void AxZkFP_OnImageReceived(object sender, IZKFPEngXEvents_OnImageReceivedEvent e)
        {
            try
            {
                int w = 256;
                int h = 288;
                using (var bmp = new Bitmap(w, h))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        int hdc = g.GetHdc().ToInt32();
                        axZkFP.PrintImageAt(hdc, 0, 0, w, h);
                        g.ReleaseHdc();
                    }
                    using (var ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);
                        lastCapturedImageBase64 = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                    }
                }

                BroadcastSse(new Dictionary<string, object>
                {
                    { "event", "image_received" },
                    { "image", lastCapturedImageBase64 }
                });
            }
            catch (Exception ex)
            {
                Log("Error procesando imagen: " + ex.Message);
            }
        }

        private void AxZkFP_OnFeatureInfo(object sender, IZKFPEngXEvents_OnFeatureInfoEvent e)
        {
            try
            {
                if (axZkFP.IsRegister)
                {
                    Beep();
                    int touchesRemaining = axZkFP.EnrollIndex - 1;
                    int quality = axZkFP.LastQuality;

                    Log(string.Format("Toma de huella recibida. Restan: {0}, Calidad: {1}", touchesRemaining, quality));

                    BroadcastSse(new Dictionary<string, object>
                    {
                        { "event", "enroll_touch" },
                        { "remaining", touchesRemaining },
                        { "quality", quality },
                        { "rawQuality", e.aQuality }
                    });
                }
            }
            catch (Exception ex)
            {
                Log("Error en OnFeatureInfo: " + ex.Message);
            }
        }

        private void AxZkFP_OnEnroll(object sender, IZKFPEngXEvents_OnEnrollEvent e)
        {
            try
            {
                if (!e.actionResult)
                {
                    Log("Registro fallido o cancelado.");
                    BroadcastSse(new Dictionary<string, object>
                    {
                        { "event", "enroll_failed" },
                        { "message", "Registro fallido. Intente de nuevo." }
                    });
                    return;
                }

                lastTemplate9 = axZkFP.GetTemplateAsStringEx("9");
                lastTemplate10 = axZkFP.GetTemplateAsStringEx("10");

                if (!string.IsNullOrEmpty(lastTemplate10))
                {
                    axZkFP.AddRegTemplateStrToFPCacheDBEx(fpcHandle, currentFpid, lastTemplate9, lastTemplate10);
                    currentFpid++;

                    Beep();
                    Log("¡Registro exitoso! Plantilla 10.0 generada (" + lastTemplate10.Length + " caracteres).");

                    BroadcastSse(new Dictionary<string, object>
                    {
                        { "event", "enroll_success" },
                        { "template10", lastTemplate10 },
                        { "template9", lastTemplate9 },
                        { "image", lastCapturedImageBase64 }
                    });
                }
                else
                {
                    Log("Error: El tamaño de la plantilla generada es cero.");
                    BroadcastSse(new Dictionary<string, object>
                    {
                        { "event", "enroll_failed" },
                        { "message", "Error al extraer plantilla 10.0" }
                    });
                }
            }
            catch (Exception ex)
            {
                Log("Error en OnEnroll: " + ex.Message);
            }
        }

        private void AxZkFP_OnCapture(object sender, IZKFPEngXEvents_OnCaptureEvent e)
        {
            try
            {
                string tpl = axZkFP.GetTemplateAsString();
                Beep();
                Log("Huella capturada en sensor.");

                BroadcastSse(new Dictionary<string, object>
                {
                    { "event", "captured" },
                    { "template", tpl },
                    { "image", lastCapturedImageBase64 }
                });
            }
            catch (Exception ex)
            {
                Log("Error en OnCapture: " + ex.Message);
            }
        }

        private void Beep()
        {
            try
            {
                axZkFP.ControlSensor(13, 1);
                Thread.Sleep(80);
                axZkFP.ControlSensor(13, 0);
            }
            catch { }
        }

        // ==========================================
        // SERVIDOR HTTP REST & SSE CON SOPORTE CORS
        // ==========================================
        private void StartHttpServer()
        {
            httpThread = new Thread(() =>
            {
                try
                {
                    File.AppendAllText("bridge_debug.log", "Iniciando HttpListener en puerto " + port + "...\r\n");
                    httpListener = new HttpListener();
                    httpListener.Prefixes.Add("http://localhost:" + port + "/");
                    httpListener.Prefixes.Add("http://127.0.0.1:" + port + "/");
                    httpListener.Start();
                    File.AppendAllText("bridge_debug.log", "HttpListener iniciado con éxito!\r\n");
                    Log("Servidor Web HTTP activo escuchando en http://localhost:" + port + "/");

                    while (isRunning)
                    {
                        var context = httpListener.GetContext();
                        ThreadPool.QueueUserWorkItem((c) => HandleHttpRequest((HttpListenerContext)c), context);
                    }
                }
                catch (Exception ex)
                {
                    File.AppendAllText("bridge_debug.log", "Catch HttpListener: " + ex.ToString() + "\r\n");
                    if (isRunning) Log("Excepción en servidor HTTP: " + ex.Message);
                }
            })
            { IsBackground = true };
            httpThread.Start();
        }

        private void HandleHttpRequest(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            var res = ctx.Response;

            // Headers CORS para permitir peticiones desde cualquier origen (React, Angular, Vue, localhost, remote)
            res.AddHeader("Access-Control-Allow-Origin", "*");
            res.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            res.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");

            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 204;
                res.Close();
                return;
            }

            string path = req.Url.AbsolutePath.ToLowerInvariant();

            if (path == "/favicon.ico")
            {
                res.StatusCode = 204;
                res.Close();
                return;
            }

            try
            {
                // Canal de eventos en vivo SSE (Server-Sent Events)
                if (path == "/api/events")
                {
                    res.ContentType = "text/event-stream";
                    res.Headers.Add("Cache-Control", "no-cache");
                    res.Headers.Add("Connection", "keep-alive");

                    lock (sseLock)
                    {
                        sseClients.Add(res);
                    }

                    // Enviar estado inicial
                    SendSseMessage(res, new Dictionary<string, object>
                    {
                        { "event", "ready" },
                        { "connected", isConnected },
                        { "port", port }
                    });

                    // Mantener la conexión abierta
                    return;
                }

                if (path == "/api/status")
                {
                    string sn = "";
                    if (driverInstalled && isConnected && axZkFP != null)
                    {
                        try { SafeInvoke(() => { sn = axZkFP.SensorSN; }); } catch {}
                    }

                    SendJsonResponse(res, new Dictionary<string, object>
                    {
                        { "success", true },
                        { "connected", isConnected },
                        { "driverInstalled", driverInstalled },
                        { "serial", sn },
                        { "engineVersion", "10.0" },
                        { "port", port }
                    });
                    return;
                }

                if (path == "/api/connect")
                {
                    bool ok = false;
                    SafeInvoke(() =>
                    {
                        if (!driverInstalled || axZkFP == null)
                        {
                            InitializeActiveX();
                        }
                        else
                        {
                            ok = ConnectDevice();
                        }
                    });
                    SendJsonResponse(res, new Dictionary<string, object> { { "success", isConnected }, { "connected", isConnected } });
                    return;
                }

                if (path == "/api/disconnect")
                {
                    if (driverInstalled) SafeInvoke(DisconnectDevice);
                    SendJsonResponse(res, new Dictionary<string, object> { { "success", true }, { "connected", false } });
                    return;
                }

                if (path == "/api/enroll/start")
                {
                    if (!driverInstalled || !isConnected || axZkFP == null)
                    {
                        SendJsonResponse(res, new Dictionary<string, object>
                        {
                            { "success", false },
                            { "message", "El lector ZKTeco no está conectado al puerto USB." }
                        });
                        return;
                    }

                    bool ok = false;
                    SafeInvoke(() =>
                    {
                        if (isConnected && axZkFP != null)
                        {
                            if (axZkFP.IsRegister) axZkFP.CancelEnroll();
                            axZkFP.EnrollCount = 3;
                            axZkFP.BeginEnroll();
                            ok = true;
                            Log("Iniciado modo de enrolamiento en sensor real (3 tomas requeridas).");
                        }
                    });

                    SendJsonResponse(res, new Dictionary<string, object>
                    {
                        { "success", ok },
                        { "message", ok ? "Coloque el dedo 3 veces sobre el lector óptico" : "Error al activar sensor" }
                    });
                    return;
                }

                if (path == "/api/enroll/cancel")
                {
                    SafeInvoke(() =>
                    {
                        if (isConnected && axZkFP != null && axZkFP.IsRegister)
                        {
                            axZkFP.CancelEnroll();
                            Log("Enrolamiento cancelado por petición web.");
                        }
                    });
                    SendJsonResponse(res, new Dictionary<string, object> { { "success", true } });
                    return;
                }

                if (path == "/api/verify" && req.HttpMethod == "POST")
                {
                    string body = "";
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        body = reader.ReadToEnd();
                    }

                    var data = json.Deserialize<Dictionary<string, object>>(body);
                    string regTemplate = data.ContainsKey("template") ? data["template"].ToString() : "";
                    string candidateTemplate = data.ContainsKey("candidate") ? data["candidate"].ToString() : "";

                    if (string.IsNullOrEmpty(candidateTemplate))
                    {
                        candidateTemplate = lastTemplate10;
                    }

                    bool match = false;
                    bool aRegFlag = false;

                    SafeInvoke(() =>
                    {
                        if (isConnected && axZkFP != null)
                        {
                            match = axZkFP.VerFingerFromStr(ref regTemplate, candidateTemplate, false, ref aRegFlag);
                        }
                    });

                    SendJsonResponse(res, new Dictionary<string, object>
                    {
                        { "success", true },
                        { "match", match }
                    });
                    return;
                }

                if (path == "/api/identify" && req.HttpMethod == "POST")
                {
                    string body = "";
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        body = reader.ReadToEnd();
                    }

                    var data = json.Deserialize<Dictionary<string, object>>(body);
                    string candidate = data.ContainsKey("candidate") ? data["candidate"].ToString() : "";
                    if (string.IsNullOrEmpty(candidate))
                    {
                        candidate = lastTemplate10;
                    }

                    object usersObj = data.ContainsKey("users") ? data["users"] : null;
                    object matchedUser = null;
                    bool found = false;

                    if (!string.IsNullOrEmpty(candidate) && usersObj is System.Collections.ArrayList)
                    {
                        var usersList = (System.Collections.ArrayList)usersObj;
                        SafeInvoke(() =>
                        {
                            if (isConnected && axZkFP != null)
                            {
                                foreach (var item in usersList)
                                {
                                    if (item is Dictionary<string, object>)
                                    {
                                        var u = (Dictionary<string, object>)item;
                                        if (u.ContainsKey("huella") || u.ContainsKey("template"))
                                        {
                                            string tpl = u.ContainsKey("huella") ? u["huella"].ToString() : u["template"].ToString();
                                            if (!string.IsNullOrEmpty(tpl))
                                            {
                                                bool regFlag = false;
                                                bool match = axZkFP.VerFingerFromStr(ref tpl, candidate, false, ref regFlag);
                                                if (match)
                                                {
                                                    matchedUser = u;
                                                    found = true;
                                                    Beep();
                                                    Log("Empleado identificado con éxito: " + (u.ContainsKey("nombre") ? u["nombre"] : "ID"));
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        });
                    }

                    SendJsonResponse(res, new Dictionary<string, object>
                    {
                        { "success", true },
                        { "match", found },
                        { "user", matchedUser }
                    });
                    return;
                }

                // Servir interfaz web de prueba
                if (path == "/" || path == "/index.html")
                {
                    ServeHtmlTestPage(res);
                    return;
                }

                res.StatusCode = 404;
                SendJsonResponse(res, new Dictionary<string, object> { { "error", "Endpoint no encontrado" } });
            }
            catch (Exception ex)
            {
                try
                {
                    res.StatusCode = 500;
                    SendJsonResponse(res, new Dictionary<string, object> { { "error", ex.Message } });
                }
                catch { }
            }
        }

        private void SendJsonResponse(HttpListenerResponse res, object data)
        {
            res.ContentType = "application/json; charset=utf-8";
            string text = json.Serialize(data);
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            res.ContentLength64 = bytes.Length;
            res.OutputStream.Write(bytes, 0, bytes.Length);
            res.Close();
        }

        private void BroadcastSse(Dictionary<string, object> data)
        {
            string payload = "data: " + json.Serialize(data) + "\n\n";
            byte[] bytes = Encoding.UTF8.GetBytes(payload);

            lock (sseLock)
            {
                for (int i = sseClients.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        var client = sseClients[i];
                        client.OutputStream.Write(bytes, 0, bytes.Length);
                        client.OutputStream.Flush();
                    }
                    catch
                    {
                        sseClients.RemoveAt(i);
                    }
                }
            }
        }

        private void SendSseMessage(HttpListenerResponse client, Dictionary<string, object> data)
        {
            try
            {
                string payload = "data: " + json.Serialize(data) + "\n\n";
                byte[] bytes = Encoding.UTF8.GetBytes(payload);
                client.OutputStream.Write(bytes, 0, bytes.Length);
                client.OutputStream.Flush();
            }
            catch { }
        }

        private void ServeHtmlTestPage(HttpListenerResponse res)
        {
            res.ContentType = "text/html; charset=utf-8";
            string html = GetHtmlContent();
            byte[] bytes = Encoding.UTF8.GetBytes(html);
            res.ContentLength64 = bytes.Length;
            res.OutputStream.Write(bytes, 0, bytes.Length);
            res.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            isRunning = false;
            try { DisconnectDevice(); } catch { }
            try { if (httpListener != null && httpListener.IsListening) httpListener.Stop(); } catch { }
            base.OnFormClosing(e);
        }

        private string GetHtmlContent()
        {
            return @"<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>ZKTeco Web Bridge - Panel de Control</title>
    <link href=""https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap"" rel=""stylesheet"">
    <style>
        :root {
            --bg: #0b0f19;
            --surface: #151b2b;
            --border: #232d42;
            --primary: #3b82f6;
            --primary-hover: #2563eb;
            --success: #10b981;
            --warning: #f59e0b;
            --danger: #ef4444;
            --text: #f1f5f9;
            --text-muted: #94a3b8;
        }
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body {
            font-family: 'Inter', -apple-system, sans-serif;
            background: var(--bg);
            color: var(--text);
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            align-items: center;
            padding: 2rem 1rem;
        }
        .container {
            width: 100%;
            max-width: 900px;
            display: flex;
            flex-direction: column;
            gap: 1.5rem;
        }
        .header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            background: var(--surface);
            padding: 1.25rem 1.75rem;
            border-radius: 16px;
            border: 1px solid var(--border);
        }
        .title-group h1 { font-size: 1.25rem; font-weight: 700; color: #fff; }
        .title-group p { font-size: 0.85rem; color: var(--text-muted); margin-top: 2px; }
        .badge {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 6px 14px;
            border-radius: 9999px;
            font-size: 0.8rem;
            font-weight: 600;
        }
        .badge-success { background: rgba(16, 185, 129, 0.15); color: #34d399; border: 1px solid rgba(16, 185, 129, 0.3); }
        .badge-danger { background: rgba(239, 68, 68, 0.15); color: #f87171; border: 1px solid rgba(239, 68, 68, 0.3); }
        .badge-dot { width: 8px; height: 8px; border-radius: 50%; background: currentColor; }
        
        .grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 1.5rem;
        }
        @media(max-width: 768px) { .grid { grid-template-columns: 1fr; } }

        .card {
            background: var(--surface);
            border: 1px solid var(--border);
            border-radius: 16px;
            padding: 1.5rem;
            display: flex;
            flex-direction: column;
            gap: 1.25rem;
        }
        .card-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            border-bottom: 1px solid var(--border);
            padding-bottom: 0.75rem;
        }
        .card-title { font-size: 1rem; font-weight: 600; }
        
        .fingerprint-display {
            background: #090d16;
            border: 2px dashed var(--border);
            border-radius: 12px;
            height: 220px;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            position: relative;
            overflow: hidden;
        }
        .fingerprint-display img {
            max-height: 100%;
            object-fit: contain;
            filter: drop-shadow(0 0 10px rgba(59, 130, 246, 0.4));
        }
        .scan-animation {
            position: absolute;
            top: 0; left: 0; right: 0;
            height: 3px;
            background: linear-gradient(90deg, transparent, #3b82f6, transparent);
            box-shadow: 0 0 15px #3b82f6;
            animation: scan 1.8s infinite alternate ease-in-out;
            display: none;
        }
        @keyframes scan {
            0% { top: 5%; }
            100% { top: 90%; }
        }

        .steps-tracker {
            display: flex;
            justify-content: space-between;
            gap: 8px;
        }
        .step-pill {
            flex: 1;
            padding: 8px 4px;
            text-align: center;
            background: #0d121f;
            border: 1px solid var(--border);
            border-radius: 8px;
            font-size: 0.75rem;
            color: var(--text-muted);
            transition: all 0.2s ease;
        }
        .step-pill.active {
            background: rgba(59, 130, 246, 0.2);
            color: #60a5fa;
            border-color: #3b82f6;
            font-weight: 600;
        }
        .step-pill.done {
            background: rgba(16, 185, 129, 0.2);
            color: #34d399;
            border-color: #10b981;
            font-weight: 600;
        }

        button.btn {
            background: var(--primary);
            color: white;
            border: none;
            padding: 10px 18px;
            border-radius: 10px;
            font-size: 0.9rem;
            font-weight: 600;
            cursor: pointer;
            transition: background 0.2s, transform 0.1s;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
        }
        button.btn:hover { background: var(--primary-hover); }
        button.btn:active { transform: scale(0.98); }
        button.btn-secondary {
            background: #1f283d;
            border: 1px solid var(--border);
        }
        button.btn-secondary:hover { background: #28344e; }
        button:disabled { opacity: 0.5; cursor: not-allowed; }

        textarea.code-box {
            width: 100%;
            height: 100px;
            background: #090d16;
            border: 1px solid var(--border);
            border-radius: 8px;
            color: #38bdf8;
            font-family: monospace;
            font-size: 0.75rem;
            padding: 8px;
            resize: none;
        }

        .log-container {
            background: #090d16;
            border: 1px solid var(--border);
            border-radius: 12px;
            padding: 12px;
            height: 140px;
            overflow-y: auto;
            font-family: monospace;
            font-size: 0.8rem;
            color: #cbd5e1;
            display: flex;
            flex-direction: column-reverse;
        }
        .log-item { margin-bottom: 4px; }
        .log-time { color: var(--text-muted); margin-right: 6px; }

        .code-snippet {
            background: #090d16;
            border: 1px solid var(--border);
            border-radius: 10px;
            padding: 12px;
            font-family: monospace;
            font-size: 0.75rem;
            color: #e2e8f0;
            white-space: pre-wrap;
            overflow-x: auto;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <!-- HEADER -->
        <div class=""header"">
            <div class=""title-group"">
                <h1>ZKTeco Web Bridge</h1>
                <p>API Local biométrica para desarrollo Web y CRM</p>
            </div>
            <div id=""statusBadge"" class=""badge badge-danger"">
                <span class=""badge-dot""></span>
                <span id=""statusText"">Desconectado</span>
            </div>
        </div>

        <div class=""grid"">
            <!-- PANEL DE ENROLAMIENTO -->
            <div class=""card"">
                <div class=""card-header"">
                    <span class=""card-title"">1. Enrolar Nueva Huella</span>
                    <button id=""btnConnect"" class=""btn btn-secondary"" style=""padding: 4px 10px; font-size: 0.75rem;"">Reconectar</button>
                </div>

                <div class=""fingerprint-display"" id=""fpBox"">
                    <div class=""scan-animation"" id=""scanLine""></div>
                    <img id=""fpImage"" src="""" style=""display:none;"" alt=""Huella"">
                    <div id=""fpPlaceholder"" style=""text-align:center; color: var(--text-muted);"">
                        <svg width=""56"" height=""56"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""1.5"" style=""margin-bottom:8px; opacity:0.6;"">
                            <path d=""M12 2a10 10 0 0 0-10 10v1a10 10 0 0 0 20 0v-1a10 10 0 0 0-10-10z""/>
                            <path d=""M12 6a6 6 0 0 0-6 6v1a6 6 0 0 0 12 0v-1a6 6 0 0 0-6-6z""/>
                            <path d=""M12 10a2 2 0 0 0-2 2v1a2 2 0 0 0 4 0v-1a2 2 0 0 0-2-2z""/>
                        </svg>
                        <p style=""font-size:0.85rem;"">Sensor en espera</p>
                    </div>
                </div>

                <div class=""steps-tracker"">
                    <div class=""step-pill"" id=""step1"">Paso 1</div>
                    <div class=""step-pill"" id=""step2"">Paso 2</div>
                    <div class=""step-pill"" id=""step3"">Paso 3</div>
                </div>

                <div style=""display:flex; gap:10px;"">
                    <button id=""btnStartEnroll"" class=""btn"" style=""flex:1;"">
                        Iniciar Registro (3 Tomas)
                    </button>
                    <button id=""btnCancelEnroll"" class=""btn btn-secondary"" style=""display:none;"">
                        Cancelar
                    </button>
                </div>

                <div>
                    <label style=""font-size:0.75rem; color:var(--text-muted); display:block; margin-bottom:4px;"">Plantilla Biométrica (ZKFinger 10.0 Base64):</label>
                    <textarea id=""templateText"" class=""code-box"" placeholder=""Aquí aparecerá el texto de la huella al finalizar el registro..."" readonly></textarea>
                    <button id=""btnCopy"" class=""btn btn-secondary"" style=""width:100%; margin-top:6px;"" disabled>
                        📋 Copiar Plantilla para tu Base de Datos
                    </button>
                </div>
            </div>

            <!-- PANEL DE VERIFICACION & API -->
            <div class=""card"">
                <div class=""card-header"">
                    <span class=""card-title"">2. Comparar / Verificar Huella</span>
                </div>

                <p style=""font-size:0.85rem; color:var(--text-muted);"">
                    Verifica si una huella guardada en tu sistema coincide con la persona frente al lector.
                </p>

                <button id=""btnVerify"" class=""btn btn-secondary"" style=""width:100%;"">
                    🔍 Verificar con Última Huella Registrada
                </button>
                <div id=""verifyResult"" style=""display:none; padding:10px; border-radius:8px; font-size:0.85rem; text-align:center; font-weight:600;""></div>

                <div class=""card-header"" style=""margin-top:10px;"">
                    <span class=""card-title"">3. Ejemplo JavaScript para tu Web</span>
                </div>
                <div class=""code-snippet"">// 1. Iniciar registro de huella
const res = await fetch('http://localhost:8080/api/enroll/start', { method: 'POST' });

// 2. Escuchar eventos en vivo desde tu página Web:
const sse = new EventSource('http://localhost:8080/api/events');
sse.onmessage = (e) => {
  const data = JSON.parse(e.data);
  if (data.event === 'enroll_touch') {
    console.log('Tomas restantes:', data.remaining);
  }
  if (data.event === 'enroll_success') {
    console.log('Huella lista para guardar en BD:', data.template10);
  }
};</div>
            </div>
        </div>

        <!-- LOG EN TIEMPO REAL -->
        <div class=""card"">
            <div class=""card-header"">
                <span class=""card-title"">Consola de Eventos en Tiempo Real</span>
            </div>
            <div class=""log-container"" id=""logContainer""></div>
        </div>
    </div>

    <script>
        const statusBadge = document.getElementById('statusBadge');
        const statusText = document.getElementById('statusText');
        const fpImage = document.getElementById('fpImage');
        const fpPlaceholder = document.getElementById('fpPlaceholder');
        const scanLine = document.getElementById('scanLine');
        const btnStartEnroll = document.getElementById('btnStartEnroll');
        const btnCancelEnroll = document.getElementById('btnCancelEnroll');
        const btnConnect = document.getElementById('btnConnect');
        const templateText = document.getElementById('templateText');
        const btnCopy = document.getElementById('btnCopy');
        const logContainer = document.getElementById('logContainer');
        const btnVerify = document.getElementById('btnVerify');
        const verifyResult = document.getElementById('verifyResult');

        function addLog(msg) {
            const time = new Date().toLocaleTimeString();
            const div = document.createElement('div');
            div.className = 'log-item';
            div.innerHTML = `<span class=""log-time"">[${time}]</span> ${msg}`;
            logContainer.prepend(div);
        }

        function setStatus(connected, serial) {
            if (connected) {
                statusBadge.className = 'badge badge-success';
                statusText.textContent = 'Conectado (N/S: ' + (serial || 'OK') + ')';
                btnStartEnroll.disabled = false;
            } else {
                statusBadge.className = 'badge badge-danger';
                statusText.textContent = 'Desconectado';
                btnStartEnroll.disabled = true;
            }
        }

        function resetSteps() {
            document.getElementById('step1').className = 'step-pill';
            document.getElementById('step2').className = 'step-pill';
            document.getElementById('step3').className = 'step-pill';
        }

        // Conectar a Server-Sent Events (SSE)
        function initSSE() {
            const es = new EventSource('/api/events');

            es.onmessage = (e) => {
                const data = JSON.parse(e.data);

                if (data.event === 'ready' || data.event === 'device_connected') {
                    setStatus(true, data.serial);
                    addLog('🟢 Lector biométrico listo');
                }

                if (data.event === 'device_disconnected') {
                    setStatus(false);
                    addLog('🔴 Lector biométrico desconectado');
                }

                if (data.event === 'image_received') {
                    fpPlaceholder.style.display = 'none';
                    fpImage.src = data.image;
                    fpImage.style.display = 'block';
                }

                if (data.event === 'enroll_touch') {
                    const rem = data.remaining;
                    addLog(`👉 Toma recibida (Calidad: ${data.quality}). Restan: ${rem}`);
                    if (rem === 2) {
                        document.getElementById('step1').className = 'step-pill done';
                        document.getElementById('step2').className = 'step-pill active';
                    } else if (rem === 1) {
                        document.getElementById('step1').className = 'step-pill done';
                        document.getElementById('step2').className = 'step-pill done';
                        document.getElementById('step3').className = 'step-pill active';
                    }
                }

                if (data.event === 'enroll_success') {
                    scanLine.style.display = 'none';
                    btnStartEnroll.style.display = 'inline-flex';
                    btnCancelEnroll.style.display = 'none';
                    document.getElementById('step1').className = 'step-pill done';
                    document.getElementById('step2').className = 'step-pill done';
                    document.getElementById('step3').className = 'step-pill done';

                    templateText.value = data.template10;
                    btnCopy.disabled = false;
                    addLog('🎉 ¡Huella dactilar registrada con éxito!');
                }

                if (data.event === 'enroll_failed') {
                    scanLine.style.display = 'none';
                    btnStartEnroll.style.display = 'inline-flex';
                    btnCancelEnroll.style.display = 'none';
                    resetSteps();
                    addLog('⚠️ Registro fallido. Intente de nuevo.');
                }
            };

            es.onerror = () => {
                setStatus(false);
            };
        }

        btnStartEnroll.onclick = async () => {
            resetSteps();
            document.getElementById('step1').className = 'step-pill active';
            scanLine.style.display = 'block';
            templateText.value = '';
            btnCopy.disabled = true;

            const res = await fetch('/api/enroll/start', { method: 'POST' });
            const data = await res.json();
            if (data.success) {
                btnStartEnroll.style.display = 'none';
                btnCancelEnroll.style.display = 'inline-flex';
                addLog('Coloque su dedo 3 veces sobre el lector óptico...');
            } else {
                alert('No se pudo iniciar el enrolamiento. Verifique el lector.');
                scanLine.style.display = 'none';
            }
        };

        btnCancelEnroll.onclick = async () => {
            await fetch('/api/enroll/cancel', { method: 'POST' });
            btnStartEnroll.style.display = 'inline-flex';
            btnCancelEnroll.style.display = 'none';
            scanLine.style.display = 'none';
            resetSteps();
            addLog('Enrolamiento cancelado.');
        };

        btnConnect.onclick = async () => {
            const res = await fetch('/api/connect', { method: 'POST' });
            const data = await res.json();
            setStatus(data.connected);
        };

        btnCopy.onclick = () => {
            templateText.select();
            navigator.clipboard.writeText(templateText.value);
            btnCopy.textContent = '✅ ¡Copiado al portapapeles!';
            setTimeout(() => { btnCopy.textContent = '📋 Copiar Plantilla para tu Base de Datos'; }, 2000);
        };

        btnVerify.onclick = async () => {
            if (!templateText.value) {
                alert('Primero registra una huella para tener una plantilla con la cual comparar.');
                return;
            }
            addLog('Coloque el dedo sobre el sensor para verificar...');
            verifyResult.style.display = 'block';
            verifyResult.style.background = 'rgba(59, 130, 246, 0.15)';
            verifyResult.style.color = '#60a5fa';
            verifyResult.textContent = 'Coloque el dedo sobre el sensor óptico...';

            // Simular verificación contra la última huella capturada
            const res = await fetch('/api/verify', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ template: templateText.value })
            });
            const data = await res.json();

            if (data.match) {
                verifyResult.style.background = 'rgba(16, 185, 129, 0.15)';
                verifyResult.style.color = '#34d399';
                verifyResult.textContent = `✅ Huella Coincide con Éxito (Puntaje: ${data.score})`;
                addLog(`✅ Verificación exitosa. Score: ${data.score}`);
            } else {
                verifyResult.style.background = 'rgba(239, 68, 68, 0.15)';
                verifyResult.style.color = '#f87171';
                verifyResult.textContent = '❌ Huella No Coincide';
                addLog('❌ Verificación fallida.');
            }
        };

        // Comprobar estado inicial
        fetch('/api/status')
            .then(r => r.json())
            .then(data => setStatus(data.connected, data.serial))
            .catch(() => setStatus(false));

        initSSE();
    </script>
</body>
</html>";
        }
    }
}
