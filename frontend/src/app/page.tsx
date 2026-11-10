'use client';

import React, { useState, useEffect, useCallback } from 'react';
import { FingerprintScanner } from '@/components/fingerprint/FingerprintScanner';
import { AttendanceClock } from '@/components/fingerprint/AttendanceClock';

interface Empleado {
  id: number;
  nombre: string;
  documento: string;
  huella: string;
  fechaRegistro: string;
}

export default function Home() {
  const [activeTab, setActiveTab] = useState<'asistencia' | 'registro'>('asistencia');
  const [empleados, setEmpleados] = useState<Empleado[]>([]);
  const [nombre, setNombre] = useState('');
  const [documento, setDocumento] = useState('');
  const [huellaTemplate, setHuellaTemplate] = useState<string | null>(null);
  const [cargando, setCargando] = useState(false);
  const [notificacion, setNotificacion] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null);

  const NEST_API = 'http://localhost:3001/api/empleados';

  const handleFingerprintCaptured = useCallback((tpl: string) => {
    setHuellaTemplate(tpl);
    setNotificacion({
      tipo: 'exito',
      texto: '¡Huella dactilar procesada con éxito! Ya puedes guardar el empleado.',
    });
  }, []);

  // Cargar lista de empleados desde NestJS
  const cargarEmpleados = async () => {
    try {
      const res = await fetch(NEST_API);
      if (res.ok) {
        const data = await res.json();
        setEmpleados(data);
      }
    } catch {
      // NestJS todavía apagado
    }
  };

  useEffect(() => {
    cargarEmpleados();
  }, []);

  // Guardar en NestJS
  const handleGuardar = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!huellaTemplate) {
      setNotificacion({
        tipo: 'error',
        texto: 'Por favor realiza la captura biométrica de las 3 tomas antes de guardar.',
      });
      return;
    }

    setCargando(true);
    setNotificacion(null);

    try {
      const res = await fetch(NEST_API, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          nombre,
          documento,
          huella: huellaTemplate,
        }),
      });

      const data = await res.json();

      if (res.ok) {
        setNotificacion({
          tipo: 'exito',
          texto: `¡Empleado "${nombre}" guardado con éxito en NestJS!`,
        });
        setNombre('');
        setDocumento('');
        setHuellaTemplate(null);
        cargarEmpleados();
      } else {
        setNotificacion({
          tipo: 'error',
          texto: data.message || 'Error al guardar empleado en el servidor.',
        });
      }
    } catch (err: any) {
      setNotificacion({
        tipo: 'error',
        texto: 'No se pudo conectar con el servidor NestJS (puerto 3001). Asegúrate de iniciarlo.',
      });
    } finally {
      setCargando(false);
    }
  };

  // Eliminar empleado
  const handleEliminar = async (id: number) => {
    if (!confirm('¿Seguro que deseas eliminar este empleado?')) return;
    try {
      await fetch(`${NEST_API}/${id}`, { method: 'DELETE' });
      cargarEmpleados();
    } catch {}
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 p-4 md:p-8">
      <div className="max-w-6xl mx-auto flex flex-col gap-8">
        {/* TOP BAR / HEADER */}
        <header className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 bg-slate-900 border border-slate-800 p-6 rounded-2xl shadow-xl">
          <div>
            <div className="flex items-center gap-3">
              <span className="text-3xl">🛡️</span>
              <div>
                <h1 className="text-xl font-bold text-white tracking-tight">SEA Biometrics CRM</h1>
                <p className="text-xs text-slate-400">Next.js 15 (Frontend) + NestJS (Backend) + ZKTeco SDK</p>
              </div>
            </div>
          </div>

          <div className="flex flex-wrap gap-3 text-xs">
            <div className="bg-slate-950 border border-slate-800 px-3.5 py-1.5 rounded-lg flex items-center gap-2">
              <span className="w-2 h-2 rounded-full bg-emerald-400" />
              <span>Next.js :3000</span>
            </div>
            <div className="bg-slate-950 border border-slate-800 px-3.5 py-1.5 rounded-lg flex items-center gap-2">
              <span className="w-2 h-2 rounded-full bg-blue-400" />
              <span>NestJS API :3001</span>
            </div>
            <div className="bg-slate-950 border border-slate-800 px-3.5 py-1.5 rounded-lg flex items-center gap-2">
              <span className="w-2 h-2 rounded-full bg-indigo-400" />
              <span>Puente ZKTeco :8080</span>
            </div>
          </div>
        </header>

        {/* NOTIFICACION */}
        {notificacion && (
          <div
            className={`p-4 rounded-xl text-sm font-medium flex items-center justify-between transition-all ${
              notificacion.tipo === 'exito'
                ? 'bg-emerald-500/10 border border-emerald-500/30 text-emerald-300'
                : 'bg-rose-500/10 border border-rose-500/30 text-rose-300'
            }`}
          >
            <span>{notificacion.texto}</span>
            <button
              onClick={() => setNotificacion(null)}
              className="text-slate-400 hover:text-white ml-4 font-bold"
            >
              ✕
            </button>
          </div>
        )}

        {/* NAVEGACIÓN POR PESTAÑAS */}
        <div className="flex border-b border-slate-800 gap-3">
          <button
            onClick={() => setActiveTab('asistencia')}
            className={`py-3 px-5 text-sm font-bold flex items-center gap-2 border-b-2 transition-all ${
              activeTab === 'asistencia'
                ? 'border-emerald-500 text-emerald-400 bg-emerald-500/10 rounded-t-xl shadow-lg'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            <span className="text-lg">⏱️</span> Reloj Checador (Marcar Asistencia)
          </button>
          <button
            onClick={() => setActiveTab('registro')}
            className={`py-3 px-5 text-sm font-bold flex items-center gap-2 border-b-2 transition-all ${
              activeTab === 'registro'
                ? 'border-blue-500 text-blue-400 bg-blue-500/10 rounded-t-xl shadow-lg'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            <span className="text-lg">👤</span> Gestión y Registro de Personal
          </button>
        </div>

        {/* PESTAÑA 1: RELOJ CHECADOR / ASISTENCIA */}
        {activeTab === 'asistencia' && (
          <AttendanceClock empleados={empleados} />
        )}

        {/* PESTAÑA 2: REGISTRO DE EMPLEADOS */}
        {activeTab === 'registro' && (
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-8">
          {/* COLUMNA IZQUIERDA: FORMULARIO + ESCÁNER (5 columnas) */}
          <div className="lg:col-span-5 flex flex-col gap-6">
            <div className="bg-slate-900 border border-slate-800 p-6 rounded-2xl shadow-xl flex flex-col gap-5">
              <div className="border-b border-slate-800 pb-3">
                <h2 className="text-base font-semibold text-white">Registro de Empleado</h2>
                <p className="text-xs text-slate-400">Datos personales y captura de huella dactilar</p>
              </div>

              <form onSubmit={handleGuardar} className="flex flex-col gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1.5">
                    Nombre Completo:
                  </label>
                  <input
                    type="text"
                    required
                    value={nombre}
                    onChange={(e) => setNombre(e.target.value)}
                    placeholder="Ej. María Rodríguez"
                    className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-3.5 py-2.5 text-sm text-white placeholder-slate-600 outline-none transition-colors"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1.5">
                    Cédula / DNI / Pasaporte:
                  </label>
                  <input
                    type="text"
                    required
                    value={documento}
                    onChange={(e) => setDocumento(e.target.value)}
                    placeholder="Ej. 18273645"
                    className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-3.5 py-2.5 text-sm text-white placeholder-slate-600 outline-none transition-colors"
                  />
                </div>

                {/* COMPONENTE BIOMÉTRICO */}
                <div className="pt-2">
                  <FingerprintScanner
                    onFingerprintCaptured={handleFingerprintCaptured}
                  />
                </div>

                <button
                  type="submit"
                  disabled={cargando || !huellaTemplate}
                  className={`w-full py-3 px-4 rounded-xl font-bold text-sm transition-all flex items-center justify-center gap-2 mt-2 ${
                    huellaTemplate
                      ? 'bg-emerald-600 hover:bg-emerald-500 text-white shadow-lg shadow-emerald-600/25 active:scale-[0.98]'
                      : 'bg-slate-800 text-slate-500 cursor-not-allowed'
                  }`}
                >
                  {cargando ? 'Guardando en NestJS...' : '💾 Guardar Empleado en Base de Datos'}
                </button>
              </form>
            </div>
          </div>

          {/* COLUMNA DERECHA: LISTADO DE EMPLEADOS (7 columnas) */}
          <div className="lg:col-span-7 flex flex-col gap-6">
            <div className="bg-slate-900 border border-slate-800 p-6 rounded-2xl shadow-xl flex flex-col gap-4">
              <div className="flex justify-between items-center border-b border-slate-800 pb-3">
                <div>
                  <h2 className="text-base font-semibold text-white">Empleados Registrados</h2>
                  <p className="text-xs text-slate-400">Datos almacenados en el servidor NestJS</p>
                </div>
                <button
                  onClick={cargarEmpleados}
                  className="text-xs bg-slate-800 hover:bg-slate-700 text-slate-300 px-3 py-1.5 rounded-lg transition-colors flex items-center gap-1.5"
                >
                  <span>🔄</span> Actualizar
                </button>
              </div>

              {empleados.length === 0 ? (
                <div className="text-center py-12 text-slate-500 text-sm">
                  <span className="text-3xl block mb-2">📋</span>
                  No hay empleados registrados aún o el servidor NestJS está apagado.
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-left border-collapse">
                    <thead>
                      <tr className="border-b border-slate-800 text-xs text-slate-400 font-semibold uppercase">
                        <th className="py-3 px-3">ID</th>
                        <th className="py-3 px-3">Nombre</th>
                        <th className="py-3 px-3">Documento</th>
                        <th className="py-3 px-3">Biometría</th>
                        <th className="py-3 px-3 text-right">Acción</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-800 text-sm">
                      {empleados.map((emp) => (
                        <tr key={emp.id} className="hover:bg-slate-800/40 transition-colors">
                          <td className="py-3 px-3 text-slate-500 font-mono text-xs">{emp.id}</td>
                          <td className="py-3 px-3 font-medium text-white">{emp.nombre}</td>
                          <td className="py-3 px-3 text-slate-300 font-mono text-xs">{emp.documento}</td>
                          <td className="py-3 px-3">
                            {emp.huella ? (
                              <span className="inline-flex items-center gap-1 bg-emerald-500/15 text-emerald-400 border border-emerald-500/30 px-2 py-0.5 rounded-md text-xs font-medium">
                                <span>✓</span> Registrada
                              </span>
                            ) : (
                              <span className="inline-flex items-center gap-1 bg-amber-500/15 text-amber-400 border border-amber-500/30 px-2 py-0.5 rounded-md text-xs font-medium">
                                Sin huella
                              </span>
                            )}
                          </td>
                          <td className="py-3 px-3 text-right">
                            <button
                              onClick={() => handleEliminar(emp.id)}
                              className="text-xs text-rose-400 hover:text-rose-300 hover:underline"
                            >
                              Eliminar
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>

            {/* CARD EXPLICATIVA DE ARQUITECTURA */}
            <div className="bg-slate-900/60 border border-slate-800/80 p-5 rounded-2xl text-xs text-slate-400 flex flex-col gap-2">
              <span className="font-semibold text-slate-200 text-sm">💡 ¿Cómo funciona esta pantalla?</span>
              <p>
                1. Al presionar <strong>"Iniciar Captura"</strong>, Next.js se conecta directamente a tu lector USB en <code className="text-blue-400">http://localhost:8080</code> mediante Server-Sent Events.
              </p>
              <p>
                2. Cada toma muestra la imagen real escaneada del sensor óptico ZKTeco en tiempo real.
              </p>
              <p>
                3. Al presionar <strong>"Guardar Empleado"</strong>, la plantilla biométrica se envía al servidor central NestJS en <code className="text-blue-400">http://localhost:3001/api/empleados</code> para almacenarse en la base de datos.
              </p>
            </div>
          </div>
        </div>
        )}
      </div>
    </div>
  );
}
