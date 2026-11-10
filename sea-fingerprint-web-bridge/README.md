# SEA - ZKTeco Fingerprint Web Bridge 🌐👆

Servidor puente HTTP/REST y Server-Sent Events (SSE) para conectar lectores biométricos USB de **ZKTeco** (ZK4500, ZK9500, Live20R, etc.) con cualquier aplicación web, CRM, ERP o sistema en el navegador (React, Vue, Angular, Vanilla JS, PHP, Node.js, etc.).

---

## 🚀 Inicio Rápido

1. Conecta tu lector de huellas ZKTeco por USB.
2. Haz doble clic en **`iniciar-servidor.bat`** (o ejecuta `FingerprintBridgeServer.exe`).
3. Se abrirá automáticamente la web de prueba en tu navegador:
   👉 **[http://localhost:8080](http://localhost:8080)**
4. ¡Listo! Podrás registrar huellas, ver la imagen en tiempo real y copiar la plantilla biométrica.

---

## 📡 Endpoints de la API (CORS Habilitado)

Cualquier página web (`localhost` o remota en producción) puede hacer peticiones sin problemas de bloqueo CORS.

| Método | Endpoint | Descripción |
|---|---|---|
| `GET` | `/api/status` | Obtiene el estado del dispositivo (conectado, N/S, versión). |
| `POST` | `/api/connect` | Conecta / inicializa el lector USB. |
| `POST` | `/api/disconnect` | Desconecta el lector. |
| `POST` | `/api/enroll/start` | Inicia el proceso de registro (solicita colocar el dedo 3 veces). |
| `POST` | `/api/enroll/cancel` | Cancela el registro en curso. |
| `POST` | `/api/verify` | Compara una plantilla con el candidato capturado. |
| `GET` | `/api/events` | **Stream SSE en tiempo real** (tomas, imagen escaneada, éxito). |

---

## 💻 Ejemplos de Integración Web

### 1. Enrolar / Registrar una huella desde tu Web (JavaScript / TypeScript / React / Vue):

```javascript
// 1. Escuchar los eventos en vivo del lector
const events = new EventSource('http://localhost:8080/api/events');

events.onmessage = (e) => {
  const data = JSON.parse(e.data);

  if (data.event === 'image_received') {
    // Imagen real escaneada en base64 para mostrar en tu web:
    document.getElementById('huella-preview').src = data.image;
  }

  if (data.event === 'enroll_touch') {
    console.log(`Tomas restantes: ${data.remaining}, Calidad: ${data.quality}`);
    alert(`Por favor coloque su dedo nuevamente. Faltan ${data.remaining} tomas.`);
  }

  if (data.event === 'enroll_success') {
    console.log('¡Huella generada con éxito!');
    console.log('Plantilla ZKFinger 10.0 (Base64):', data.template10);

    // Enviar la plantilla a tu backend / base de datos del CRM:
    guardarEnBaseDeDatos(data.template10);
    events.close();
  }

  if (data.event === 'enroll_failed') {
    alert('El registro falló. Intente de nuevo.');
  }
};

// 2. Disparar el inicio del registro al hacer clic en un botón
async function iniciarEnrolamiento() {
  const resp = await fetch('http://localhost:8080/api/enroll/start', {
    method: 'POST'
  });
  const res = await resp.json();
  if (res.success) {
    console.log('Sensor activado. Ponga su dedo 3 veces.');
  }
}
```

### 2. Verificar / Comparar una huella guardada:

```javascript
async function verificarHuella(plantillaGuardadaEnBD) {
  const resp = await fetch('http://localhost:8080/api/verify', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ template: plantillaGuardadaEnBD })
  });

  const res = await resp.json();
  if (res.match) {
    alert('✅ Acceso permitido: La huella coincide');
  } else {
    alert('❌ Acceso denegado: Huella no reconocida');
  }
}
```

---

## 🛠️ Modificar o Recompilar

Si deseas agregar nuevos endpoints o personalizar la lógica en C#, edita `BridgeServer.cs` y luego ejecuta:
```bat
compilar.bat
```
El script utiliza el compilador nativo de Windows (`csc.exe`) sin necesidad de instalar SDKs adicionales.
