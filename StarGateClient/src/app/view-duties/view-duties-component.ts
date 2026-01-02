import { Component, Inject, OnInit } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { PersonService } from '../services/person.service';
import { DutyService } from '../services/duty-service';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';

interface AstronautDuty {
  DutyTite: string;
  DutyStartDate: Date;
  DutyEndDate: Date | null
}
interface Person {
  CurrentRank: string;
  Name: number;
  CurrentDuty: number;
  CareerStartDate: Date;
  CareerEndDate: Date | null;
  AstronautDuties: AstronautDuty[];
}

@Component({
  selector: 'view-duties',
  templateUrl: './view-duties-component.html',
  styleUrl: './view-duties.component.css',
  standalone: false
})
export class ViewDutiesComponent implements OnInit {

  displayedColumns: string[] = [
    'DutyTitle',
    'DutyStartDate',
    'DutyEndDate'
  ];

  personName: string = "";
  showDutyForm: boolean = false;
   
  dataSource!: MatTableDataSource<any[]>;
  constructor(
    private dutyService: DutyService,
    private _dialogRef: MatDialogRef<ViewDutiesComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
  ) { }

  ngOnInit() {
    this.getDuties();
  }

  getDuties() {

    this.dutyService.getDutiesByName(this.data).subscribe({
      next: (res) => {
        this.personName = res.person.name;
        this.dataSource = new MatTableDataSource(res.astronautDuties);
      },
      error: (err: any) => {
        var thiserror = err;
      }
    });
  }

  showDuties(data: any) {
    this.showDutyForm = true;
  }
}
