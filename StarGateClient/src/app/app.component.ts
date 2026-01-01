import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { ViewDutiesComponent } from './view-duties/view-duties-component'; 
import { Component, OnInit, ViewChild } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { PersonService } from './services/person.service';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';

interface Person {
  CurrentRank: string;
  Name: number;
  CurrentDuty: number;
  CareerStartDate: Date;
  CareerEndDate: Date | null;
}

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  standalone: false,
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {

  displayedColumns: string[] = [
    'CurrentRank',
    'Name',
    'CurrentDuty',
    'CareerStartDate',
    'CareerEndDate',
    'ViewDuties'
  ];
  dataSource!: MatTableDataSource<any[]>;

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  constructor(private _dialog: MatDialog, private personService: PersonService
  ) {
    const dialogConfig = new MatDialogConfig();
    dialogConfig.panelClass = 'my-custom-dialog-class'; 
  }

  ngOnInit() {

    this.getPeople();
  }

  getPeople() {
    this.personService.getPeople().subscribe({
      next: (res) => {
        this.dataSource = new MatTableDataSource(res.people);
        this.dataSource.sort = this.sort;
        this.dataSource.paginator = this.paginator;
      },
      error: (err: any) => {
        //with no time to implement front end logging solution, just assuming splunk or other log aggregation will pick up console text from the container
        console.error(err);
      }
    });
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();

    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
  }

  showDuties(data: any) {

    const dialogRef = this._dialog.open(ViewDutiesComponent, {
      data,
    });
  }
}
