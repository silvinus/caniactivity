import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-schedule',
  templateUrl: './schedule.component.html',
  styleUrls: ['./schedule.component.scss']
})
export class ScheduleComponent implements OnInit {
  appointmentsData: Appointment[] = [];
  currentDate: Date = new Date(2021, 3, 29);

  constructor() { }

  ngOnInit(): void {
  }

}

export class Appointment {
  text: string = "";

  startDate: Date = new Date(2021, 3, 29);

  endDate: Date = new Date(2021, 3, 29);

  allDay?: boolean;
}
