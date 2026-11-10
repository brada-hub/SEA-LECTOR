export interface Asistencia {
  id: number;
  empleadoId: number;
  nombre: string;
  documento: string;
  fechaHora: Date;
  tipo: 'ENTRADA' | 'SALIDA';
}
