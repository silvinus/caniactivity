import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-activities',
  templateUrl: './activities.component.html',
  styleUrls: ['./activities.component.scss']
})
export class ActivitiesComponent {

  constructor() { }

  screen: Function = (width: any) => {
    return (width < 1200) ? 'sm' : 'lg';
  }


}
