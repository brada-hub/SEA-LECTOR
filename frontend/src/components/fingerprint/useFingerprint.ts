'use client';

import { useState, useEffect, useCallback, useRef } from 'react';

export interface FingerprintState {
  isConnected: boolean;
  isEnrolling: boolean;
  touchesRemaining: number;
  liveImage: string | null;
  template: string | null;
  lastCapturedTemplate: string | null;
  statusMessage: string;
  error: string | null;
}

export function useFingerprint(bridgeUrl = 'http://localhost:8080') {
  const [state, setState] = useState<FingerprintState>({
    isConnected: false,
    isEnrolling: false,
    touchesRemaining: 3,
    liveImage: null,
    template: null,
    lastCapturedTemplate: null,
    statusMessage: 'Buscando lector de huellas...',
    error: null,
  });

  const eventSourceRef = useRef<EventSource | null>(null);

  const checkStatus = useCallback(async () => {
    try {
      const res = await fetch(`${bridgeUrl}/api/status`);
      const data = await res.json();
      setState((prev) => ({
        ...prev,
        isConnected: Boolean(data.connected),
        statusMessage: data.connected ? 'Lector ZKTeco conectado y listo' : 'Lector USB no detectado',
        error: null,
      }));
    } catch {
      setState((prev) => ({
        ...prev,
        isConnected: false,
        statusMessage: 'Puente biométrico apagado (ejecuta iniciar-servidor.bat)',
        error: 'No se pudo conectar a ' + bridgeUrl,
      }));
    }
  }, [bridgeUrl]);

  useEffect(() => {
    checkStatus();

    try {
      const es = new EventSource(`${bridgeUrl}/api/events`);
      eventSourceRef.current = es;

      es.onmessage = (e) => {
        try {
          const data = JSON.parse(e.data);

          if (data.event === 'ready' || data.event === 'device_connected') {
            setState((prev) => ({
              ...prev,
              isConnected: true,
              statusMessage: 'Lector ZKTeco listo',
              error: null,
            }));
          }

          if (data.event === 'device_disconnected') {
            setState((prev) => ({
              ...prev,
              isConnected: false,
              statusMessage: 'Lector desconectado',
            }));
          }

          if (data.event === 'captured') {
            setState((prev) => ({
              ...prev,
              liveImage: data.image || prev.liveImage,
              lastCapturedTemplate: data.template,
              statusMessage: 'Huella capturada en sensor',
            }));
          }

          if (data.event === 'image_received') {
            setState((prev) => ({
              ...prev,
              liveImage: data.image,
            }));
          }

          if (data.event === 'enroll_touch') {
            setState((prev) => ({
              ...prev,
              touchesRemaining: data.remaining,
              statusMessage: `Toma recibida (Calidad: ${data.quality}). Restan ${data.remaining} tomas.`,
            }));
          }

          if (data.event === 'enroll_success') {
            setState((prev) => ({
              ...prev,
              isEnrolling: false,
              touchesRemaining: 0,
              template: data.template10,
              statusMessage: '¡Huella dactilar capturada con éxito!',
            }));
          }

          if (data.event === 'enroll_failed') {
            setState((prev) => ({
              ...prev,
              isEnrolling: false,
              statusMessage: data.message || 'Registro fallido. Intente de nuevo.',
              error: data.message,
            }));
          }
        } catch {}
      };

      es.onerror = () => {
        setState((prev) => ({ ...prev, isConnected: false }));
      };
    } catch {}

    return () => {
      if (eventSourceRef.current) {
        eventSourceRef.current.close();
      }
    };
  }, [bridgeUrl, checkStatus]);

  const startEnroll = useCallback(async () => {
    setState((prev) => ({
      ...prev,
      isEnrolling: true,
      touchesRemaining: 3,
      template: null,
      liveImage: null,
      statusMessage: 'Coloque su dedo 3 veces sobre el lector óptico...',
      error: null,
    }));

    try {
      const res = await fetch(`${bridgeUrl}/api/enroll/start`, { method: 'POST' });
      const data = await res.json();
      if (!data.success) {
        setState((prev) => ({
          ...prev,
          isEnrolling: false,
          error: data.message || 'No se pudo iniciar el lector',
          statusMessage: 'Error al iniciar sensor',
        }));
      }
    } catch (err: any) {
      setState((prev) => ({
        ...prev,
        isEnrolling: false,
        error: err.message,
        statusMessage: 'Error de comunicación con el puente local',
      }));
    }
  }, [bridgeUrl]);

  const cancelEnroll = useCallback(async () => {
    try {
      await fetch(`${bridgeUrl}/api/enroll/cancel`, { method: 'POST' });
    } catch {}
    setState((prev) => ({
      ...prev,
      isEnrolling: false,
      statusMessage: 'Registro cancelado',
    }));
  }, [bridgeUrl]);

  const resetTemplate = useCallback(() => {
    setState((prev) => ({
      ...prev,
      template: null,
      liveImage: null,
      statusMessage: 'Lector listo para nueva captura',
    }));
  }, []);

  const identifyUser = useCallback(
    async (
      users: Array<{ id: number; nombre: string; documento: string; huella: string }>,
      candidateTpl?: string
    ) => {
      try {
        const res = await fetch(`${bridgeUrl}/api/identify`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            candidate: candidateTpl || '',
            users,
          }),
        });
        return await res.json();
      } catch (err: any) {
        return { success: false, match: false, error: err.message };
      }
    },
    [bridgeUrl]
  );

  return {
    ...state,
    startEnroll,
    cancelEnroll,
    resetTemplate,
    checkStatus,
    identifyUser,
  };
}
