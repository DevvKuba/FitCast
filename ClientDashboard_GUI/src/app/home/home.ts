import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LoginComponent } from "../login/login.component";
import { ButtonDirective, ButtonModule } from "primeng/button";
import { RegisterComponent } from "../register/register.component";
import { HomeNavbarComponent } from "../home-navbar/home-navbar.component";
import { CommonModule } from '@angular/common';
import { ChartModule } from 'primeng/chart';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { Chart, registerables } from 'chart.js';

// Register Chart.js components
Chart.register(...registerables);

@Component({
  selector: 'app-home',
  imports: [ButtonModule, HomeNavbarComponent, CommonModule, ChartModule, CardModule, TableModule, TagModule, RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home implements OnInit {
  // Analytics Chart Data
  revenueChartData: any;
  workoutChartData: any;
  chartOptions: any;

  // Sample data for feature previews
  sampleClients: any[] = [];
  sampleWorkouts: any[] = [];
  samplePayments: any[] = [];

  ngOnInit() {
    this.initializeCharts();
    this.loadSampleData();
  }

  initializeCharts() {
    const documentStyle = getComputedStyle(document.documentElement);
    const textColor = documentStyle.getPropertyValue('--text-color');
    const textColorSecondary = documentStyle.getPropertyValue('--text-color-secondary');
    const surfaceBorder = documentStyle.getPropertyValue('--surface-border');

    // Revenue Analytics Chart
    this.revenueChartData = {
      labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
      datasets: [
        {
          label: 'Revenue (GBP)',
          data: [1200, 1900, 1500, 2100, 1800, 2400, 2200, 2600, 2100, 2300, 1840, 1950],
          fill: true,
          backgroundColor: 'rgba(34, 197, 94, 0.2)',
          borderColor: 'rgba(34, 197, 94, 1)',
          tension: 0.4
        }
      ]
    };

    // Workout Activity Chart
    this.workoutChartData = {
      labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
      datasets: [
        {
          label: 'Workouts Logged',
          data: [32, 45, 38, 52, 48, 61, 55, 68, 52, 58, 47, 54],
          backgroundColor: 'rgba(79, 143, 211, 0.7)',
        }
      ]
    };

    this.chartOptions = {
      maintainAspectRatio: false,
      aspectRatio: 0.6,
      plugins: {
        legend: {
          position: 'top',
          labels: {
            color: textColor
          }
        }
      },
      scales: {
        x: {
          ticks: {
            color: textColorSecondary
          },
          grid: {
            color: surfaceBorder
          }
        },
        y: {
          ticks: {
            color: textColorSecondary
          },
          grid: {
            color: surfaceBorder
          }
        }
      }
    };
  }

  loadSampleData() {
    // Sample Clients Data
    this.sampleClients = [
      { id: 1, firstName: 'Bob', currentBlockSession: 3, totalBlockSessions: 4, isActive: true },
      { id: 2, firstName: 'Michael', currentBlockSession: 2, totalBlockSessions: 4, isActive: true },
      { id: 3, firstName: 'Bella', currentBlockSession: 3, totalBlockSessions: 4, isActive: true },
      { id: 4, firstName: 'Lewis', currentBlockSession: 7, totalBlockSessions: 8, isActive: true },
      { id: 5, firstName: 'Janet', currentBlockSession: 2, totalBlockSessions: 3, isActive: false }
    ];

    // Sample Workouts Data
    this.sampleWorkouts = [
      { 
        id: 1, 
        workoutTitle: 'Bob Full Body', 
        clientName: 'Bob', 
        currentBlockSession: 3,
        totalBlockSessions: 4,
        sessionDate: '2026-01-28',
        duration: 62,
        exerciseCount: 6
      },
      { 
        id: 2, 
        workoutTitle: 'Lewis Upper Body Routine', 
        clientName: 'Lewis', 
        currentBlockSession: 7,
        totalBlockSessions: 8,
        sessionDate: '2026-01-28',
        duration: 53,
        exerciseCount: 6
      },
      { 
        id: 3, 
        workoutTitle: 'Bella Lower Body', 
        clientName: 'Bella', 
        currentBlockSession: 3,
        totalBlockSessions: 4,
        sessionDate: '2026-01-28',
        duration: 66,
        exerciseCount: 6
      },
      { 
        id: 4, 
        workoutTitle: 'Lewis Full Body', 
        clientName: 'Lewis', 
        currentBlockSession: 6,
        totalBlockSessions: 8,
        sessionDate: '2026-01-24',
        duration: 60,
        exerciseCount: 9
      }
    ];

    // Sample Payments Data
    this.samplePayments = [
      { 
        id: 1, 
        clientName: 'Bob', 
        amount: 160.00, 
        currency: 'GBP', 
        numberOfSessions: 4, 
        paymentDate: '2025-12-18',
        confirmed: true 
      },
      { 
        id: 2, 
        clientName: 'Michael', 
        amount: 160.00, 
        currency: 'GBP', 
        numberOfSessions: 4, 
        paymentDate: '2025-12-10',
        confirmed: true 
      },
      { 
        id: 3, 
        clientName: 'Lewis', 
        amount: 320.00, 
        currency: 'GBP', 
        numberOfSessions: 8, 
        paymentDate: '2025-12-04',
        confirmed: true 
      },
      { 
        id: 4, 
        clientName: 'Bella', 
        amount: 160.00, 
        currency: 'GBP', 
        numberOfSessions: 4, 
        paymentDate: '2025-11-28',
        confirmed: false 
      }
    ];
  }
}
