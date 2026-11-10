import { Controller, Post, Get, Body } from '@nestjs/common';
import { AsistenciasService } from './asistencias.service.js';

@Controller('api/asistencias')
export class AsistenciasController {
  constructor(private readonly asistenciasService: AsistenciasService) {}

  @Post('marcar')
  async marcar(@Body() body: { empleadoId: number; nombre: string; documento: string }) {
    const asistencia = await this.asistenciasService.marcar(
      body.empleadoId,
      body.nombre,
      body.documento,
    );
    return {
      success: true,
      mensaje: `Asistencia (${asistencia.tipo}) registrada con éxito.`,
      asistencia,
    };
  }

  @Get()
  async listar() {
    return this.asistenciasService.listar();
  }
}
