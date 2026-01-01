import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class PersonService {
  constructor(private _http: HttpClient) { }

  addEmployee(data: any): Observable<any> {
    return this._http.post('http://localhost:3000/employees', data);
  }

  updateEmployee(id: number, data: any): Observable<any> {
    return this._http.put(`http://localhost:3000/employees/${id}`, data);
  }

  getPeople(): Observable<any> {

    var result = this._http.get('https://localhost:44339/person');

    return result;
  }

  deleteEmployee(id: number): Observable<any> {
    return this._http.delete(`http://localhost:3000/employees/${id}`);
  }
}
