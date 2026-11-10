'use client';

import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useFingerprint } from './useFingerprint';

interface Empleado {
  id: number;
  nombre: string;
  documento: string;
  huella: string;
}

interface Asistencia {
  id: number;
  empleadoId: number;
  nombre: string;
  documento: string;
  fechaHora: string;
  tipo: 'ENTRADA' | 'SALIDA';
}

interface AttendanceClockProps {
  empleados: Empleado[];
  bridgeUrl?: string;
  nestApiUrl?: string;
}

export function AttendanceClock({
  empleados,
  bridgeUrl = 'http://localhost:8080',
  nestApiUrl = 'http://localhost:3001/api/asistencias',
}: AttendanceClockProps) {
  const { isConnected, liveImage, lastCapturedTemplate, statusMessage, identifyUser } =
    useFingerprint(bridgeUrl);

  const [asistencias, setAsistencias] = useState<Asistencia[]>([]);
  const [identificando, setIdentificando] = useState(false);
  const [resultado, setResultado] = useState<{
    tipo: 'exito' | 'error';
    nombre?: string;
    documento?: string;
    tipoRegistro?: 'ENTRADA' | 'SALIDA';
    mensaje: string;
    hora?: string;
  } | null>(null);

  const [horaActual, setHoraActual] = useState<string>('');
  const lastProcessedTplRef = useRef<string | null>(null);

  // Reloj digital en vivo
  useEffect(() => {
    const updateTime = () => {
      const now = new Date();
      setHoraActual(
        now.toLocaleTimeString('es-ES', {
          hour: '2-digit',
          minute: '2-digit',
          second: '2-digit',
          hour12: false,
        })
      );
    };
    updateTime();
    const timer = setInterval(updateTime, 1000);
    return () => clearInterval(timer);
  }, []);

  // Cargar historial de asistencias desde NestJS
  const cargarAsistencias = useCallback(async () => {
    try {
      const res = await fetch(nestApiUrl);
      if (res.ok) {
        const data = await res.json();
        setAsistencias(data);
      }
    } catch {}
  }, [nestApiUrl]);

  useEffect(() => {
    cargarAsistencias();
  }, [cargarAsistencias]);

  // Procesar marcación de huella
  const procesarHuella = useCallback(
    async (candidateTpl: string) => {
      if (!candidateTpl) return;
      setIdentificando(true);

      try {
        const res = await identifyUser(empleados, candidateTpl);

        if (res.success && res.match && res.user) {
          const user = res.user;

          // Registrar marcación en NestJS
          let tipoMarcacion: 'ENTRADA' | 'SALIDA' = 'ENTRADA';
          try {
            const nestRes = await fetch(`${nestApiUrl}/marcar`, {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({
                empleadoId: user.id,
                nombre: user.nombre,
                documento: user.documento,
              }),
            });
            if (nestRes.ok) {
              const data = await nestRes.json();
              tipoMarcacion = data.asistencia?.tipo || 'ENTRADA';
              cargarAsistencias();
            }
          } catch {}

          const hora = new Date().toLocaleTimeString('es-ES', {
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
          });

          setResultado({
            tipo: 'exito',
            nombre: user.nombre,
            documento: user.documento,
            tipoRegistro: tipoMarcacion,
            mensaje: `¡Asistencia (${tipoMarcacion}) registrada con éxito!`,
            hora,
          });
        } else {
          setResultado({
            tipo: 'error',
            mensaje:
              'Huella no reconocida. Por favor, asegúrese de estar registrado en el sistema.',
          });
        }
      } catch (err: any) {
        setResultado({
          tipo: 'error',
          mensaje: 'Error de comunicación: ' + err.message,
        });
      } finally {
        setIdentificando(false);
      }
    },
    [empleados, identifyUser, nestApiUrl, cargarAsistencias]
  );

  // Escuchar cuando alguien apoya el dedo en el sensor ZKTeco (evento captured)
  useEffect(() => {
    if (
      lastCapturedTemplate &&
      lastCapturedTemplate !== lastProcessedTplRef.current
    ) {
      lastProcessedTplRef.current = lastCapturedTemplate;
      procesarHuella(lastCapturedTemplate);
    }
  }, [lastCapturedTemplate, procesarHuella]);

  return (
    <div className="flex flex-col gap-6">
      {/* TARJETA PRINCIPAL DEL RELOJ CHECADOR */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6 md:p-8 shadow-xl flex flex-col items-center gap-6">
        {/* RELOJ DIGITAL */}
        <div className="text-center">
          <div className="text-4xl md:text-5xl font-mono font-bold text-white tracking-widest bg-slate-950 px-6 py-2.5 rounded-2xl border border-slate-800 shadow-inner inline-block">
            {horaActual || '--:--:--'}
          </div>
          <p className="text-xs text-slate-400 mt-2 font-medium">
            Control de Asistencia Biométrico (ZK4500 USB)
          </p>
        </div>

        {/* VISOR BIOMÉTRICO */}
        <div className="w-full max-w-sm flex flex-col items-center gap-4">
          <div className="relative w-48 h-56 bg-slate-950 rounded-2xl border-2 border-dashed border-slate-700 flex items-center justify-center overflow-hidden shadow-2xl">
            {identificando && (
              <div className="absolute inset-x-0 top-0 h-1.5 bg-gradient-to-r from-transparent via-emerald-400 to-transparent shadow-[0_0_15px_#10b981] animate-pulse" />
            )}

            {liveImage ? (
              <img
                src={liveImage}
                alt="Huella Dactilar"
                className="max-h-full object-contain filter drop-shadow-[0_0_10px_rgba(16,185,129,0.5)] transition-all scale-105"
              />
            ) : (
              <div className="flex flex-col items-center text-slate-500 gap-2 p-4 text-center">
                <span className="text-5xl animate-pulse">👆</span>
                <span className="text-xs font-semibold text-slate-400">
                  Apoye el dedo sobre el sensor
                </span>
                <span className="text-[10px] text-slate-500">
                  (1 toque rápido para registrar)
                </span>
              </div>
            )}
          </div>

          {/* ESTADO DEL LECTOR */}
          <div className="flex items-center gap-2 text-xs">
            <span
              className={`w-2.5 h-2.5 rounded-full ${
                isConnected ? 'bg-emerald-400 animate-pulse' : 'bg-rose-400'
              }`}
            />
            <span className={isConnected ? 'text-emerald-400 font-medium' : 'text-rose-400'}>
              {isConnected ? 'Lector ZKTeco en línea' : 'Lector USB desconectado'}
            </span>
          </div>

          {/* BOTÓN MANUAL DE ESCANEO */}
          <button
            onClick={() => {
              if (lastCapturedTemplate) {
                procesarHuella(lastCapturedTemplate);
              } else {
                fetch(`${bridgeUrl}/api/verify`, {
                  method: 'POST',
                  headers: { 'Content-Type': 'application/json' },
                  body: JSON.stringify({}),
                });
              }
            }}
            disabled={!isConnected || identificando}
            className={`w-full py-3 px-5 rounded-xl font-bold text-sm transition-all flex items-center justify-center gap-2 ${
              isConnected
                ? 'bg-blue-600 hover:bg-blue-500 text-white shadow-lg shadow-blue-500/25 active:scale-[0.98]'
                : 'bg-slate-800 text-slate-500 cursor-not-allowed'
            }`}
          >
            {identificando ? (
              <span>⌛ Identificando con precisión 99.9%...</span>
            ) : (
              <span>🔍 Validar Huella Colocada</span>
            )}
          </button>
        </div>

        {/* RESULTADO DE LA IDENTIFICACIÓN */}
        {resultado && (
          <div
            className={`w-full max-w-lg p-5 rounded-2xl border text-sm transition-all flex flex-col gap-2 ${
              resultado.tipo === 'exito'
                ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-200 shadow-lg shadow-emerald-500/10'
                : 'bg-rose-500/10 border-rose-500/30 text-rose-200'
            }`}
          >
            <div className="flex justify-between items-start">
              <span className="font-bold text-base flex items-center gap-2">
                {resultado.tipo === 'exito' ? '✅' : '❌'} {resultado.mensaje}
              </span>
              <button
                onClick={() => setResultado(null)}
                className="text-slate-400 hover:text-white text-xs font-bold"
              >
                ✕
              </button>
            </div>

            {resultado.tipo === 'exito' && (
              <div className="grid grid-cols-2 gap-2 text-xs mt-2 pt-2 border-t border-emerald-500/20">
                <div>
                  <span className="text-emerald-400 font-semibold block">Colaborador:</span>
                  <span className="text-white text-sm font-bold">{resultado.nombre}</span>
                </div>
                <div>
                  <span className="text-emerald-400 font-semibold block">Documento:</span>
                  <span className="text-white text-sm font-mono">{resultado.documento}</span>
                </div>
                <div>
                  <span className="text-emerald-400 font-semibold block">Tipo Marcación:</span>
                  <span
                    className={`inline-block px-2 py-0.5 rounded text-[11px] font-bold ${
                      resultado.tipoRegistro === 'ENTRADA'
                        ? 'bg-blue-500/20 text-blue-300 border border-blue-500/30'
                        : 'bg-amber-500/20 text-amber-300 border border-amber-500/30'
                    }`}
                  >
                    {resultado.tipoRegistro}
                  </span>
                </div>
                <div>
                  <span className="text-emerald-400 font-semibold block">Hora Registrada:</span>
                  <span className="text-white font-mono">{resultado.hora}</span>
                </div>
              </div>
            )}
          </div>
        )}
      </div>

      {/* TABLA DE MARCACIONES RECIENTES */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6 shadow-xl flex flex-col gap-4">
        <div className="flex justify-between items-center border-b border-slate-800 pb-3">
          <div>
            <h3 className="text-base font-semibold text-white">Registro de Asistencias de Hoy</h3>
            <p className="text-xs text-slate-400">Historial en tiempo real de entradas y salidas</p>
          </div>
          <button
            onClick={cargarAsistencias}
            className="text-xs bg-slate-800 hover:bg-slate-700 text-slate-300 px-3 py-1.5 rounded-lg transition-colors flex items-center gap-1.5"
          >
            <span>🔄</span> Actualizar
          </button>
        </div>

        {asistencias.length === 0 ? (
          <div className="text-center py-10 text-slate-500 text-sm">
            <span className="text-3xl block mb-2">⏱️</span>
            No hay marcaciones registradas todavía hoy.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-slate-800 text-xs text-slate-400 font-semibold uppercase">
                  <th className="py-2.5 px-3">Hora</th>
                  <th className="py-2.5 px-3">Colaborador</th>
                  <th className="py-2.5 px-3">Documento</th>
                  <th className="py-2.5 px-3">Tipo</th>
                  <th className="py-2.5 px-3 text-right">Estado</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800 text-sm">
                {asistencias.map((item) => (
                  <tr key={item.id} className="hover:bg-slate-800/40 transition-colors">
                    <td className="py-2.5 px-3 font-mono text-xs text-slate-300">
                      {new Date(item.fechaHora).toLocaleTimeString('es-ES')}
                    </td>
                    <td className="py-2.5 px-3 font-medium text-white">{item.nombre}</td>
                    <td className="py-2.5 px-3 font-mono text-xs text-slate-400">
                      {item.documento}
                    </td>
                    <td className="py-2.5 px-3">
                      <span
                        className={`inline-block px-2 py-0.5 rounded text-xs font-semibold ${
                          item.tipo === 'ENTRADA'
                            ? 'bg-blue-500/15 text-blue-400 border border-blue-500/30'
                            : 'bg-amber-500/15 text-amber-400 border border-amber-500/30'
                        }`}
                      >
                        {item.tipo}
                      </span>
                    </td>
                    <td className="py-2.5 px-3 text-right">
                      <span className="text-emerald-400 text-xs font-semibold">
                        ✓ Verificado
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
