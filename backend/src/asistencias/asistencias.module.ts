import { Module } from '@nestjs/common';
import { AsistenciasController } from './asistencias.controller.js';
import { AsistenciasService } from './asistencias.service.js';

@Module({
  controllers: [AsistenciasController],
  providers: [AsistenciasService],
  exports: [AsistenciasService],
})
export class AsistenciasModule {}
