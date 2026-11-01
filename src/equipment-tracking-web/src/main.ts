import { bootstrapApplication } from '@angular/platform-browser';
import { Component } from '@angular/core';

@Component({selector:'app-root',standalone:true,template:`<main><h1>Equipment Tracking</h1><p>Seeded review environment</p><p>API readiness: <strong>local</strong></p></main>`,styles:[`main{font-family:system-ui;max-width:60rem;margin:4rem auto;padding:1rem}h1{color:#164e63}`]})
class AppComponent {}
bootstrapApplication(AppComponent).catch(console.error);
