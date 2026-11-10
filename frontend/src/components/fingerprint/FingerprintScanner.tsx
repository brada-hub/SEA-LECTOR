'use client';

import React, { useEffect } from 'react';
import { useFingerprint } from './useFingerprint';

interface FingerprintScannerProps {
  onFingerprintCaptured?: (templateBase64: string) => void;
  bridgeUrl?: string;
}

export function FingerprintScanner({
  onFingerprintCaptured,
  bridgeUrl = 'http://localhost:8080',
}: FingerprintScannerProps) {
  const {
    isConnected,
    isEnrolling,
    touchesRemaining,
    liveImage,
    template,
    statusMessage,
    error,
    startEnroll,
    cancelEnroll,
    resetTemplate,
    checkStatus,
  } = useFingerprint(bridgeUrl);

  const lastEmittedTemplateRef = React.useRef<string | null>(null);

  useEffect(() => {
    if (template && template !== lastEmittedTemplateRef.current) {
      lastEmittedTemplateRef.current = template;
      onFingerprintCaptured?.(template);
    } else if (!template) {
      lastEmittedTemplateRef.current = null;
    }
  }, [template, onFingerprintCaptured]);

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6 shadow-xl flex flex-col gap-4 text-slate-100">
      {/* Header con Estado */}
      <div className="flex justify-between items-start">
        <div>
          <h3 className="text-base font-semibold text-white flex items-center gap-2">
            <span>👆</span> Lector ZKTeco
          </h3>
          <p className="text-xs text-slate-400 mt-1">{statusMessage}</p>
        </div>
        <div
          className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold ${
            isConnected
              ? 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/30'
              : 'bg-rose-500/15 text-rose-400 border border-rose-500/30'
          }`}
        >
          <span
            className={`w-2 h-2 rounded-full ${
              isConnected ? 'bg-emerald-400 animate-pulse' : 'bg-rose-400'
            }`}
          />
          {isConnected ? 'Conectado' : 'Desconectado'}
        </div>
      </div>

      {/* Recuadro de Visualización de la Huella */}
      <div className="relative h-48 bg-slate-950 rounded-xl border-2 border-dashed border-slate-800 flex items-center justify-center overflow-hidden">
        {isEnrolling && (
          <div className="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-transparent via-blue-500 to-transparent shadow-[0_0_12px_#3b82f6] animate-pulse" />
        )}

        {liveImage ? (
          <img
            src={liveImage}
            alt="Huella Dactilar"
            className="max-h-full object-contain filter drop-shadow-[0_0_8px_rgba(59,130,246,0.5)]"
          />
        ) : (
          <div className="flex flex-col items-center text-slate-500">
            <svg
              className={`w-12 h-12 mb-2 transition-transform duration-300 ${
                isEnrolling ? 'text-blue-500 scale-110 animate-bounce' : 'text-slate-600'
              }`}
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.5"
            >
              <path d="M12 2a10 10 0 0 0-10 10v1a10 10 0 0 0 20 0v-1a10 10 0 0 0-10-10z" />
              <path d="M12 6a6 6 0 0 0-6 6v1a6 6 0 0 0 12 0v-1a6 6 0 0 0-6-6z" />
              <path d="M12 10a2 2 0 0 0-2 2v1a2 2 0 0 0 4 0v-1a2 2 0 0 0-2-2z" />
            </svg>
            <span className="text-xs">
              {isEnrolling ? 'Coloque el dedo en el sensor...' : 'Esperando inicio'}
            </span>
          </div>
        )}
      </div>

      {/* Pasos de las 3 Tomas */}
      <div className="grid grid-cols-3 gap-2">
        <div className={`text-center py-2 text-xs rounded-lg font-medium transition-all ${getStepClass(touchesRemaining, 1, isEnrolling, Boolean(template))}`}>
          Toma 1
        </div>
        <div className={`text-center py-2 text-xs rounded-lg font-medium transition-all ${getStepClass(touchesRemaining, 2, isEnrolling, Boolean(template))}`}>
          Toma 2
        </div>
        <div className={`text-center py-2 text-xs rounded-lg font-medium transition-all ${getStepClass(touchesRemaining, 3, isEnrolling, Boolean(template))}`}>
          Toma 3
        </div>
      </div>

      {/* Alerta de Error */}
      {error && (
        <div className="bg-rose-500/10 border border-rose-500/20 text-rose-300 text-xs px-3 py-2 rounded-lg">
          ⚠️ {error}
        </div>
      )}

      {/* Botones de Acción */}
      <div className="flex flex-col gap-2">
        {!isEnrolling && !template && (
          <button
            type="button"
            onClick={startEnroll}
            disabled={!isConnected}
            className={`w-full py-2.5 px-4 rounded-xl font-semibold text-sm transition-all flex items-center justify-center gap-2 ${
              isConnected
                ? 'bg-blue-600 hover:bg-blue-500 text-white shadow-lg shadow-blue-500/20 active:scale-[0.98]'
                : 'bg-slate-800 text-slate-500 cursor-not-allowed'
            }`}
          >
            <span>👆</span> Iniciar Captura de Huella
          </button>
        )}

        {isEnrolling && (
          <button
            type="button"
            onClick={cancelEnroll}
            className="w-full py-2.5 px-4 rounded-xl font-semibold text-sm bg-rose-600 hover:bg-rose-500 text-white transition-all active:scale-[0.98]"
          >
            Cancelar Captura
          </button>
        )}

        {template && (
          <div className="flex items-center justify-between bg-emerald-500/10 border border-emerald-500/20 px-3.5 py-2.5 rounded-xl">
            <span className="text-emerald-400 text-xs font-semibold flex items-center gap-1.5">
              <span>✅</span> Huella Biométrica Lista
            </span>
            <button
              type="button"
              onClick={resetTemplate}
              className="text-xs bg-slate-800 hover:bg-slate-700 text-slate-300 px-2.5 py-1 rounded-md transition-colors"
            >
              Recapturar
            </button>
          </div>
        )}

        {!isConnected && (
          <button
            type="button"
            onClick={checkStatus}
            className="w-full py-2 px-3 rounded-lg text-xs bg-slate-800 hover:bg-slate-700 text-slate-300 transition-colors"
          >
            🔄 Reintentar Detección
          </button>
        )}
      </div>
    </div>
  );
}

function getStepClass(remaining: number, stepNumber: number, isEnrolling: boolean, isDone: boolean) {
  if (isDone) {
    return 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/40 font-semibold';
  }
  if (!isEnrolling) {
    return 'bg-slate-950 text-slate-600 border border-slate-800';
  }
  if (stepNumber === 1) {
    return remaining < 3
      ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/40 font-semibold'
      : 'bg-blue-500/20 text-blue-400 border border-blue-500/40 font-semibold animate-pulse';
  }
  if (stepNumber === 2) {
    if (remaining < 2) return 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/40 font-semibold';
    if (remaining === 2) return 'bg-blue-500/20 text-blue-400 border border-blue-500/40 font-semibold animate-pulse';
    return 'bg-slate-950 text-slate-600 border border-slate-800';
  }
  if (stepNumber === 3) {
    if (remaining === 1) return 'bg-blue-500/20 text-blue-400 border border-blue-500/40 font-semibold animate-pulse';
    return 'bg-slate-950 text-slate-600 border border-slate-800';
  }
  return 'bg-slate-950 text-slate-600 border border-slate-800';
}
