# SEA LECTOR - Sistema Biométrico Web Completo 🛡️👆

Ecosistema completo para lectura, registro y autenticación biométrica de huellas dactilares **ZKTeco** en la web.

---

## 🏗️ Estructura del Proyecto

```
c:\SEA LECTOR\
│
├── 🚀 iniciar-todo.bat          <-- Doble clic aquí para encender todo con 1 clic
│
├── 📁 frontend\                 <-- Next.js 15 (React, Tailwind CSS, TypeScript)
│   ├── src\app\page.tsx         <-- Pantalla de CRM con formulario y tabla de empleados
│   └── src\components\          <-- Componente visual <FingerprintScanner />
│
├── 📁 backend\                  <-- NestJS (API REST, TypeScript)
│   └── src\empleados\           <-- Módulo de empleados y almacenamiento de huellas
│
└── 📁 sea-fingerprint-web-bridge\ <-- Servidor Puente C#
    ├── FingerprintBridgeServer.exe  <-- Comunicación nativa con el lector USB
    └── BridgeServer.cs              <-- Código fuente en C#
```

---

## ⚡ Cómo Ejecutar Todo el Sistema

1. Conecta tu lector USB ZKTeco a la computadora.
2. Haz doble clic en:
   👉 **`iniciar-todo.bat`**
3. El script levantará automáticamente:
   * **Puente Biométrico ZKTeco** en `http://localhost:8080`
   * **Backend NestJS** en `http://localhost:3001`
   * **Frontend Next.js** en `http://localhost:3000`
4. Se abrirá tu navegador en **`http://localhost:3000`** con la aplicación lista para registrar empleados y capturar huellas en tiempo real.
