using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using AxZKFPEngXControl;

namespace sea_zkteco_fingerprint
{
    public partial class FormMain : Form
    {
        private AxZKFPEngX axZKFPEngX1;
        private int fpcHandle;
        private int FPID = 1;
        private int FMatchType = 2; // 1 = 1:1 Verification, 2 = 1:N Identification
        private string sRegTemplate = "";
        private string sRegTemplate10 = "";
        private System.ComponentModel.IContainer components = null;

        // UI Controls
        private Label label1;
        private Label label2;
        private RichTextBox templateBox;
        private Button copyTemplateBtn;
        private RadioButton template10Rb;
        private RadioButton template9Rb;
        private Button startRegisterBtn;
        private Button restartRegisterBtn;
        private Button connectBtn;
        private Button disconnectBtn;
        private Label statusLb;
        private Label deviceLb;
        private Label serialDeviceLb;
        private PictureBox fingerPrintPicture;

        public FormMain()
        {
            InitializeComponent();
            _InitializeAxZkFP();
        }

        private void Form_Load(object sender, EventArgs e)
        {
            // Por defecto selecciona la versión 10.0
            template10Rb.Checked = true;
        }

        private void _InitializeAxZkFP()
        {
            axZKFPEngX1 = new AxZKFPEngX();
            ((System.ComponentModel.ISupportInitialize)(axZKFPEngX1)).BeginInit();

            // Suscripción a eventos del SDK ZKTeco
            axZKFPEngX1.OnFeatureInfo += new IZKFPEngXEvents_OnFeatureInfoEventHandler(_AxZKFPEngX1_OnFeatureInfo);
            axZKFPEngX1.OnImageReceived += new IZKFPEngXEvents_OnImageReceivedEventHandler(_AxZKFPEngX1_OnImageReceived);
            axZKFPEngX1.OnEnroll += new IZKFPEngXEvents_OnEnrollEventHandler(_AxZKFPEngX1_OnEnroll);
            axZKFPEngX1.OnCapture += new IZKFPEngXEvents_OnCaptureEventHandler(_AxZKFPEngX1_OnCapture);

            Controls.Add(axZKFPEngX1);
            ((System.ComponentModel.ISupportInitialize)(axZKFPEngX1)).EndInit();
        }

        private void connectBtn_Click(object sender, EventArgs e)
        {
            // Inicializar motor biométrico
            if (axZKFPEngX1.InitEngine() == 0)
            {
                _EnableButtons(false);

                // Configurar versión del algoritmo (9.0 o 10.0)
                if (template9Rb.Checked)
                {
                    axZKFPEngX1.FPEngineVersion = "9";
                }
                else
                {
                    axZKFPEngX1.FPEngineVersion = "10";
                }

                // Crear base de datos en caché para identificación rápida en memoria
                fpcHandle = axZKFPEngX1.CreateFPCacheDBEx();

                // Obtener número de serie del sensor USB
                string serial = axZKFPEngX1.SensorSN;
                serialDeviceLb.Text = string.IsNullOrEmpty(serial) ? "" : serial;
                deviceLb.Text = "Conectado";

                connectBtn.Enabled = false;
                MessageBox.Show("Dispositivo conectado!", "Información de conexión");
            }
            else
            {
                connectBtn.Enabled = true;
                axZKFPEngX1.EndEngine();
                MessageBox.Show("Conexión fallida!", "Información de conexión");
            }

            FMatchType = 2; // Modo 1:N
        }

        private void disconnectBtn_Click(object sender, EventArgs e)
        {
            axZKFPEngX1.EndEngine();
            _EnableButtons(true);
            connectBtn.Enabled = true;
        }

        private void startRegisterBtn_Click(object sender, EventArgs e)
        {
            if (axZKFPEngX1.IsRegister)
            {
                axZKFPEngX1.CancelEnroll();
            }

            // Requiere 3 muestras para enrolar
            axZKFPEngX1.EnrollCount = 3;
            axZKFPEngX1.BeginEnroll();

            statusLb.Text = "Por favor, proporcione su huella dactilar.";
            templateBox.Text = string.Empty;
            copyTemplateBtn.Enabled = false;
        }

        // Evento que dibuja la huella en el PictureBox en tiempo real
        private void _AxZKFPEngX1_OnImageReceived(object sender, IZKFPEngXEvents_OnImageReceivedEvent e)
        {
            Bitmap bmp = new Bitmap(fingerPrintPicture.Width, fingerPrintPicture.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                int hdc = g.GetHdc().ToInt32();
                axZKFPEngX1.PrintImageAt(hdc, 0, 0, bmp.Width, bmp.Height);
                g.ReleaseHdc();
            }
            fingerPrintPicture.Image = bmp;
        }

        // Evento cuando se pasa el dedo durante el enrolamiento (pasos 1, 2 y 3)
        private void _AxZKFPEngX1_OnFeatureInfo(object sender, IZKFPEngXEvents_OnFeatureInfoEvent e)
        {
            string msg = string.Empty;
            if (axZKFPEngX1.EnrollIndex != 1)
            {
                if (axZKFPEngX1.IsRegister)
                {
                    _BeepSound();

                    int touchesRemaining = axZKFPEngX1.EnrollIndex - 1;
                    if (touchesRemaining > 0)
                    {
                        msg = "Por favor, proporcione su huella dactilar nuevamente... " + touchesRemaining.ToString();
                    }

                    msg += ", Calidad=";
                    int quality = axZKFPEngX1.LastQuality;

                    if (e.aQuality == -1)
                    {
                        msg += "no bueno, huella sospechosa, Certeza=" + quality.ToString();
                    }
                    else if (e.aQuality > 0)
                    {
                        msg += "no bueno, Certeza=" + quality.ToString();
                    }
                    else
                    {
                        msg += "bueno, Certeza=" + quality.ToString();
                    }

                    statusLb.Text = msg;
                }
            }
        }

        // Evento cuando termina el proceso de registro (tras las 3 muestras)
        private void _AxZKFPEngX1_OnEnroll(object sender, IZKFPEngXEvents_OnEnrollEvent e)
        {
            if (e.actionResult == 0)
            {
                MessageBox.Show("Registro fallido!", "Información de Registro");
                return;
            }

            // Extraer plantillas generadas
            sRegTemplate = axZKFPEngX1.GetTemplateAsStringEx("9");
            sRegTemplate10 = axZKFPEngX1.GetTemplateAsStringEx("10");

            if (!string.IsNullOrEmpty(sRegTemplate) && sRegTemplate.Length > 0)
            {
                statusLb.Text = "...";

                if (!string.IsNullOrEmpty(sRegTemplate10) && sRegTemplate10.Length > 0)
                {
                    // Guardar en la base de datos de memoria
                    axZKFPEngX1.AddRegTemplateStrToFPCacheDBEx(fpcHandle, FPID, sRegTemplate, sRegTemplate10);
                }
                else
                {
                    MessageBox.Show("Registro 10.0 fallido, tamaño del resultado es cero.", "Información de Registro");
                }

                // Decodificar y guardar archivo fingerprint.tpl
                object template;
                if (template9Rb.Checked)
                {
                    template = axZKFPEngX1.DecodeTemplate1(sRegTemplate);
                    axZKFPEngX1.SetTemplateLen(ref template, 602);
                }
                else
                {
                    template = axZKFPEngX1.DecodeTemplate1(sRegTemplate10);
                }

                axZKFPEngX1.SaveTemplate("fingerprint.tpl", template);

                FPID++;
                templateBox.Text = sRegTemplate10; // Texto base64 del template
                copyTemplateBtn.Enabled = true;
                fingerPrintPicture.Image = null;
                _BeepSound();

                MessageBox.Show("Registro exitoso, copiar información generada.", "Información de Registro");
            }
            else
            {
                MessageBox.Show("Registro fallido, tamaño del resultado es cero.", "Información de Registro");
            }
        }

        // Evento cuando se captura una huella para validación / identificación
        private void _AxZKFPEngX1_OnCapture(object sender, IZKFPEngXEvents_OnCaptureEvent e)
        {
            if (FMatchType == 1) // 1:1 Verificación
            {
                int score = 0;
                string currentTemplate = axZKFPEngX1.GetTemplateAsString();
                string regTpl = template9Rb.Checked ? sRegTemplate : sRegTemplate10;

                bool match = axZKFPEngX1.VerFingerFromStr(ref regTpl, currentTemplate, false, ref score);
                statusLb.Text = match ? "Validación exitosa" : "Validación fallida";
            }
            else if (FMatchType == 2) // 1:N Identificación
            {
                int score = 8;
                int processedNum = 1;
                int matchedId = axZKFPEngX1.IdentificationInFPCacheDB(fpcHandle, e.aTemplate, ref score, ref processedNum);

                if (matchedId == -1)
                {
                    statusLb.Text = "Identificación fallida";
                }
                else
                {
                    statusLb.Text = string.Format("Identificación Exitosa ID = {0} Score = {1}  Número Procesado = {2}.", matchedId, score, processedNum);
                }
            }
        }

        private void copyTemplateBtn_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(templateBox.Text))
            {
                Clipboard.SetText(templateBox.Text);
                MessageBox.Show("Información en portapapeles, ejecute pegar / ctrl + v", "Información de Registro.");
            }
        }

        private void _BeepSound()
        {
            // Emite un pitido utilizando el sensor óptico ZKTeco
            axZKFPEngX1.ControlSensor(13, 1);
            Thread.Sleep(100);
            axZKFPEngX1.ControlSensor(13, 0);
        }

        private void _EnableButtons(bool enabled)
        {
            connectBtn.Enabled = enabled;
            disconnectBtn.Enabled = !enabled;
            startRegisterBtn.Enabled = !enabled;
            template10Rb.Enabled = enabled;
        }
    }
}
