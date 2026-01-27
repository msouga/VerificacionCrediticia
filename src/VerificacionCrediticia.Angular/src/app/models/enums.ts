export enum EstadoCrediticio {
  Normal = 'Normal',
  ConProblemasPotenciales = 'ConProblemasPotenciales',
  Moroso = 'Moroso',
  EnCobranza = 'EnCobranza',
  Castigado = 'Castigado',
  SinInformacion = 'SinInformacion'
}

export enum Severidad {
  Baja = 'Baja',
  Media = 'Media',
  Alta = 'Alta',
  Critica = 'Critica'
}

export enum TipoAlerta {
  ScoreBajo = 'ScoreBajo',
  Morosidad = 'Morosidad',
  DeudaVencida = 'DeudaVencida',
  ProblemaLegal = 'ProblemaLegal',
  EmpresaInactiva = 'EmpresaInactiva',
  InformacionIncompleta = 'InformacionIncompleta',
  RelacionRiesgosa = 'RelacionRiesgosa',
  ProblemaDetectado = 'ProblemaDetectado'
}

export enum Recomendacion {
  Aprobar = 'Aprobar',
  RevisarManualmente = 'RevisarManualmente',
  Rechazar = 'Rechazar'
}

export enum TipoNodo {
  Persona = 'Persona',
  Empresa = 'Empresa'
}
