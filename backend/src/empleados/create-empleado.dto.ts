export class CreateEmpleadoDto {
  nombre!: string;
  documento!: string;
  huella!: string; // Base64 de la plantilla ZKFinger 10.0
}
