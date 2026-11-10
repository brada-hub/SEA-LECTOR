import { Controller, Post, Get, Delete, Body, Param, ParseIntPipe } from '@nestjs/common';
import { EmpleadosService } from './empleados.service.js';
import { CreateEmpleadoDto } from './create-empleado.dto.js';

@Controller('api/empleados')
export class EmpleadosController {
  constructor(private readonly empleadosService: EmpleadosService) {}

  @Post()
  async registrar(@Body() dto: CreateEmpleadoDto) {
    const empleado = await this.empleadosService.registrar(dto);
    return {
      success: true,
      mensaje: 'Empleado registrado con éxito.',
      empleado,
    };
  }

  @Get()
  async listar() {
    return this.empleadosService.listar();
  }

  @Get(':documento')
  async buscarPorDoc(@Param('documento') documento: string) {
    return this.empleadosService.buscarPorDocumento(documento);
  }

  @Delete(':id')
  async eliminar(@Param('id', ParseIntPipe) id: number) {
    const ok = await this.empleadosService.eliminar(id);
    return { success: ok };
  }
}
