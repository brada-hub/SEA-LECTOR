import { Injectable, NotFoundException } from '@nestjs/common';
import { CreateEmpleadoDto } from './create-empleado.dto.js';

export interface Empleado {
  id: number;
  nombre: string;
  documento: string;
  huella: string;
  fechaRegistro: Date;
}

@Injectable()
export class EmpleadosService {
  private empleados: Empleado[] = [
    {
      id: 1,
      nombre: 'Administrador Demo',
      documento: '10000001',
      huella: '',
      fechaRegistro: new Date(),
    },
  ];
  private nextId = 2;

  async registrar(dto: CreateEmpleadoDto): Promise<Empleado> {
    const nuevo: Empleado = {
      id: this.nextId++,
      nombre: dto.nombre,
      documento: dto.documento,
      huella: dto.huella,
      fechaRegistro: new Date(),
    };
    this.empleados.push(nuevo);
    console.log(`[NestJS] Nuevo empleado registrado: ${nuevo.nombre} (Doc: ${nuevo.documento})`);
    return nuevo;
  }

  async listar(): Promise<Empleado[]> {
    return this.empleados;
  }

  async buscarPorDocumento(documento: string): Promise<Empleado> {
    const emp = this.empleados.find((e) => e.documento === documento);
    if (!emp) {
      throw new NotFoundException(`No existe empleado con documento ${documento}`);
    }
    return emp;
  }

  async eliminar(id: number): Promise<boolean> {
    const index = this.empleados.findIndex((e) => e.id === id);
    if (index === -1) return false;
    this.empleados.splice(index, 1);
    return true;
  }
}
