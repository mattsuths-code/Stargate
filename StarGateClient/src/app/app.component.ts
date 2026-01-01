import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { PersonService } from './services/person.service';

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
    'CareerEndDate'
  ];
  dataSource!: MatTableDataSource<any[]>;
  constructor(private http: HttpClient, private personService: PersonService) { }

  ngOnInit() {
    this.getPeople();
  }

  getPeople() {
    this.personService.getPeople().subscribe({
      next: (res) => {
        this.dataSource = new MatTableDataSource(res.people);
      },
      error: (err: any) => {
        var thiserror = err;
      }
    });
  }
}
