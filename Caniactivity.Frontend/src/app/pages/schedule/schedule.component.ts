import { Component, OnInit, ViewChild } from '@angular/core';
import notify from 'devextreme/ui/notify';
import { UserService } from '../../shared/services/user.service';
import * as AspNetData from 'devextreme-aspnet-data-nojquery';
import { environment } from '../../../environments/environment';
import { AppointmentService, Dog } from '../../shared/services/appointment.service';
import { Observable } from 'rxjs';
import { DxSchedulerComponent } from 'devextreme-angular';

@Component({
  selector: 'app-schedule',
  templateUrl: './schedule.component.html',
  styleUrls: ['./schedule.component.scss']
})
export class ScheduleComponent {
  appointmentsData: any;
  currentDate: Date = new Date();
  views = ['day', 'week', 'month'];
  currentView = this.views[1];
  cache: any = [];
  @ViewChild(DxSchedulerComponent) scheduler: DxSchedulerComponent | undefined;

  statuses: any[] = [
  {
    text: 'Validé',
    id: 0,
    color: '#cc5c53',
  }, {
    text: 'Non validé',
    id: 1,
    color: '#ff9747',
  },
];


  constructor(private userService: UserService, private appointments: AppointmentService) {
    this.appointmentsData = AspNetData.createStore({
      key: 'id',
      loadUrl: `${environment.apiUrl}/api/appointment`,
      insertUrl: `${environment.apiUrl}/api/appointment`,
      updateUrl: `${environment.apiUrl}/api/appointment`,
      deleteUrl: `${environment.apiUrl}/api/appointment`,
      onBeforeSend(method, ajaxOptions) {
        ajaxOptions.headers = {
          "Authorization": `Bearer ${localStorage.getItem('token')}`
        };
      },
    });
  }

  async validate(e: any, appointement: any) {
    this.stopPropagation(e.event);
    this.scheduler?.instance.hideAppointmentTooltip();
    this.scheduler?.instance.updateAppointment(appointement, { ...appointement, ...{ status: 1 }});
  }
  remove(e: any, appointement: any) {
    this.stopPropagation(e.event);
    this.scheduler?.instance.deleteAppointment(appointement);
    this.scheduler?.instance.hideAppointmentTooltip();
  }
  edit(e: any, appointement: any) {
    this.scheduler?.instance.showAppointmentPopup(
      appointement,
      false
    );
  }
  stopPropagation(e: any) {
    e.stopPropagation();
  }

  onOptionChanged(e: any) {
    if (e.name === 'currentView') {
      this.currentView = e.value;
    }
  }

  async onAppointmentFormOpening(e: any) {
    let startDate = new Date(e.appointmentData.startDate);
    if (!this.isValidAppointmentDate(startDate)) {
      e.cancel = true;
      this.notifyDisableDate();
    }
    const that = this;
    const form = e.form;
    form.option('items', []);
    let dogs = await that.userService.getValidateDogs() || [];

    form.option('labelMode', 'static');
    form.option('items', [
      {
        label: {
          text: 'Demande de créneau',
          visible: false
        },
        editorType: 'dxTextBox',
        colSpan: 2,
        editorOptions: {
          disabled: true,
          value: 'Demande de créneau'
        }
      },
      {
        label: {
          text: 'Chien',
        },
        editorType: 'dxTagBox',
        dataField: 'dogs',
        colSpan: 2,
        editorOptions: {
          items: dogs,
          displayExpr: 'name',
          valueExpr: 'id',
          value: (e.appointmentData.dogs || []).map((w: any) => w.id)
        },
      },
      {
        itemType: "empty",
        colSpan: 2
      },
      {
        label: {
          text: 'Date de début',
        },
        dataField: 'startDate',
        editorType: 'dxDateBox',
        editorOptions: {
          width: '100%',
          type: 'datetime',
          onValueChanged(args: any) {
            startDate = new Date(args.value);
            let range = form.getEditor("rangeHour").option("value");
            if (range == 1) {
              form.updateData('endDate', new Date(startDate.getTime() + 30 * 60 * 1000));
            }
            else {
              form.updateData('endDate', new Date(startDate.getTime() + 60 * 60 * 1000));
            }
          },
        },
      },
      {
        label: {
          text: 'Date de fin',
        },
        name: 'endDate',
        dataField: 'endDate',
        editorType: 'dxDateBox',
        editorOptions: {
          disabled: true,
          width: '100%',
          type: 'datetime'
        },
      },
      {
        label: {
          text: 'Plage horaire',
        },
        name: 'rangeHour',
        editorType: 'dxRadioGroup',
        colSpan: 2,
        editorOptions: {
          items: [{ id: 1, label: "30 minutes" }, { id: 2, label: "1 heure" }],
          displayExpr: 'label',
          valueExpr: 'id',
          value: new Date(e.appointmentData.endDate).getTime() - startDate.getTime() == 1800000 ? 1 : 2,
          layout: 'horizontal',
          onValueChanged(args: any) {
            startDate = new Date(form.getEditor("startDate").option("value"));
            if (args.value == 1) {
              form.updateData('endDate', new Date(startDate.getTime() + 30 * 60 * 1000));
            }
            else {
              form.updateData('endDate', new Date(startDate.getTime() + 60 * 60 * 1000));
            }
          },
        },
      }
    ]);

    this.applyDisableDatesToDateEditors(e.form);
  }

  onAppointmentAdding(e: any) {
    const isValidAppointment = this.isValidAppointment(e.component, e.appointmentData);
    if (!isValidAppointment) {
      e.cancel = true;
      this.notifyDisableDate();
    }
  }

  onAppointmentUpdating(e: any) {
    const isValidAppointment = this.isValidAppointment(e.component, e.newData);
    if (!isValidAppointment) {
      e.cancel = true;
      this.notifyDisableDate();
    }
  }

  notifyDisableDate() {
    notify('Le créneau sélectionné n\'est pas dans les horaires d\'ouverture', 'warning', 1000);
  }

  isHoliday(date: Date) {
    //const localeDate = date.toLocaleDateString();
    //const holidays = this.dataService.getHolidays();
    //return holidays.filter((holiday) => holiday.toLocaleDateString() === localeDate).length > 0;
    return false;
  }

  isWeekend(date: Date) {
    const day = date.getDay();
    return day === 0 /*|| day === 6*/;
  }

  isDisableDate(date: Date) {
    return this.isHoliday(date) || this.isWeekend(date) || this.isInThePast(date);
  }

  isDisabledDateCell(date: Date) {
    return this.isMonthView()
      ? this.isWeekend(date)
      : this.isDisableDate(date);
  }

  isDinner(date: Date) {
    const hours = date.getHours();
    const dinnerTime = { from: 12, to: 13 }; // this.dataService.getDinnerTime();
    return hours >= dinnerTime.from && hours < dinnerTime.to;
  }

  isInThePast(date: Date) {
    return date < new Date();
  }

  hasCoffeeCupIcon(date: Date) {
    const hours = date.getHours();
    const minutes = date.getMinutes();
    const dinnerTime = { from: 12, to: 13 }; // this.dataService.getDinnerTime();

    return hours === dinnerTime.from && minutes === 0;
  }

  isMonthView() {
    return this.currentView === 'month';
  }

  isValidAppointment(component: any, appointmentData: any) {
    const startDate = new Date(appointmentData.startDate);
    const endDate = new Date(appointmentData.endDate);
    const cellDuration = component.option('cellDuration');
    return this.isValidAppointmentInterval(startDate, endDate, cellDuration);
  }

  isValidAppointmentInterval(startDate: Date, endDate: Date, cellDuration: number) {
    const edgeEndDate = new Date(endDate.getTime() - 1);

    if (!this.isValidAppointmentDate(edgeEndDate)) {
      return false;
    }

    const durationInMs = cellDuration * 60 * 1000;
    const date = startDate;
    while (date <= endDate) {
      if (!this.isValidAppointmentDate(date)) {
        return false;
      }
      const newDateTime = date.getTime() + durationInMs - 1;
      date.setTime(newDateTime);
    }

    return true;
  }

  isValidAppointmentDate(date: Date) {
    return !this.isHoliday(date) && !this.isDinner(date) && !this.isWeekend(date) && !this.isInThePast(date);
  }

  applyDisableDatesToDateEditors(form: any) {
    const holidays: any = []; // this.dataService.getHolidays();
    const startDateEditor = form.getEditor('startDate');
    startDateEditor.option('disabledDates', holidays);

    const endDateEditor = form.getEditor('endDate');
    endDateEditor.option('disabledDates', holidays);
  }
}

export class Appointment {
  text: string = "";

  startDate: Date = new Date(2021, 3, 29);

  endDate: Date = new Date(2021, 3, 29);

  allDay?: boolean;
}
