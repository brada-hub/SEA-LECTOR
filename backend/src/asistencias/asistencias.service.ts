import { Injectable } from '@nestjs/common';
import { Asistencia } from './asistencia.interface.js';

@Injectable()
export class AsistenciasService {
  private asistencias: Asistencia[] = [];
  private nextId = 1;

  async marcar(empleadoId: number, nombre: string, documento: string): Promise<Asistencia> {
    // Determinar si la última marcación fue Entrada o Salida para alternar
    const previas = this.asistencias.filter((a) => a.empleadoId === empleadoId);
    const ultima = previas[previas.length - 1];
    const tipo: 'ENTRADA' | 'SALIDA' = (!ultima || ultima.tipo === 'SALIDA') ? 'ENTRADA' : 'SALIDA';

    const nueva: Asistencia = {
      id: this.nextId++,
      empleadoId,
      nombre,
      documento,
      fechaHora: new Date(),
      tipo,
    };

    this.asistencias.unshift(nueva); // Más recientes primero
    console.log(`[NestJS Asistencia] ${tipo} registrada para: ${nombre} (${documento}) a las ${nueva.fechaHora.toLocaleTimeString()}`);
    return nueva;
  }

  async listar(): Promise<Asistencia[]> {
    return this.asistencias;
  }
}
