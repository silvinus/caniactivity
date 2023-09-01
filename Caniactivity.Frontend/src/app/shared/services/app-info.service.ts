import { Injectable } from '@angular/core';

@Injectable()
export class AppInfoService {
  constructor() {}

  public get title() {
    return 'Caniactivity : Parc de loisirs canin';
  }

  public get currentYear() {
    return new Date().getFullYear();
  }

  public get providers() {
    return [{ id: 0, name: 'local' }, { id: 1, name: 'Google' }];
  }

  public get statuses() {
    return [{ id: 0, name: 'En cours d\'inscription' }, { id: 1, name: 'Validé' }, { id: 2, name: 'Refusé' }];
  }

  public get dogStatuses() {
    return [{ id: 0, name: 'Test sociabilité en attente' }, { id: 1, name: 'Test sociabilité validé' }, { id: 2, name: 'Test sociabilité refusé' }];
  }
}
