import { Module } from '@nestjs/common';
import { AppController } from './app.controller.js';
import { AppService } from './app.service.js';
import { EmpleadosModule } from './empleados/empleados.module.js';
import { AsistenciasModule } from './asistencias/asistencias.module.js';

@Module({
  imports: [EmpleadosModule, AsistenciasModule],
  controllers: [AppController],
  providers: [AppService],
})
export class AppModule {}
