import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class PersonService {
  constructor(private _http: HttpClient) { }

  addEmployee(data: any): Observable<any> {
    return this._http.post('https://localhost:44339/person', data);
  }

  getPeople(): Observable<any> {

    return this._http.get('https://localhost:44339/person');
  }
}
