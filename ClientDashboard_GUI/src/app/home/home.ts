import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HomeNavbarComponent } from "../home-navbar/home-navbar.component";
import { CommonModule } from '@angular/common';
import { ChartModule } from 'primeng/chart';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { AvatarModule } from 'primeng/avatar';
import { Chart, ChartData, ChartOptions, registerables } from 'chart.js';

Chart.register(...registerables);

interface PlatformFeature {
  icon: string;
  title: string;
  description: string;
}

interface SampleClient {
  name: string;
  initials: string;
  avatarBg: string;
  lastActive: string;
  isActive: boolean;
}

interface SampleWorkout {
  workoutTitle: string;
  clientName: string;
  clientInitials: string;
  avatarBg: string;
  exerciseCount: number;
  durationMinutes: number;
}

interface SamplePayment {
  clientName: string;
  initials: string;
  avatarBg: string;
  amount: number;
  numberOfSessions: number;
  confirmed: boolean;
}

@Component({
  selector: 'app-home',
  imports: [HomeNavbarComponent, CommonModule, ChartModule, CardModule, TableModule, TagModule, AvatarModule, RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home implements OnInit {

  // Illustrative platform overview - not wired to live data.
  platformFeatures: PlatformFeature[] = [
    { icon: 'pi-users', title: 'Client Management', description: 'Track & manage all clients' },
    { icon: 'pi-chart-line', title: 'Workout Tracking', description: 'Log workout sessions for each client' },
    { icon: 'pi-pound', title: 'Payment Processing', description: 'Monitor revenue & payments' },
    { icon: 'pi-bell', title: 'In-App Notifications', description: 'Alerts for sessions, step tracking & more' },
    { icon: 'pi-send', title: 'SMS Reminders', description: 'Automated client & trainer text alerts and reminders' }
  ];

  sampleClients: SampleClient[] = [];
  sampleWorkouts: SampleWorkout[] = [];
  samplePayments: SamplePayment[] = [];

  activityPatternsChartData: ChartData<'bar'> | undefined;
  activityPatternsChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false }
    },
    scales: {
      x: { grid: { display: false } },
      y: { beginAtZero: true, ticks: { precision: 0 } }
    }
  };

  ngOnInit() {
    this.loadSampleData();
    this.buildActivityPatternsChart();
  }

  loadSampleData() {
    this.sampleClients = [
      { name: 'Bob Smith', initials: 'BS', avatarBg: 'bg-primary', lastActive: 'Today', isActive: true },
      { name: 'Michael D.', initials: 'MD', avatarBg: 'bg-secondary', lastActive: 'Yesterday', isActive: true },
      { name: 'Bella C.', initials: 'BC', avatarBg: 'bg-tertiary-container', lastActive: '3 days ago', isActive: false }
    ];

    this.sampleWorkouts = [
      { workoutTitle: 'Full Body Power', clientName: 'Bob Smith', clientInitials: 'BS', avatarBg: 'bg-primary', exerciseCount: 12, durationMinutes: 45 },
      { workoutTitle: 'Upper Body Routine', clientName: 'Lewis Hamilton', clientInitials: 'LH', avatarBg: 'bg-secondary', exerciseCount: 8, durationMinutes: 30 },
      { workoutTitle: 'Lower Body', clientName: 'Bella C.', clientInitials: 'BC', avatarBg: 'bg-tertiary-container', exerciseCount: 10, durationMinutes: 40 }
    ];

    this.samplePayments = [
      { clientName: 'Bob Smith', initials: 'BS', avatarBg: 'bg-primary', amount: 120, numberOfSessions: 4, confirmed: true },
      { clientName: 'Lewis Hamilton', initials: 'LH', avatarBg: 'bg-secondary', amount: 240, numberOfSessions: 8, confirmed: true },
      { clientName: 'Janet W.', initials: 'JW', avatarBg: 'bg-tertiary-container', amount: 120, numberOfSessions: 4, confirmed: false }
    ];
  }

  buildActivityPatternsChart() {
    this.activityPatternsChartData = {
      labels: ['SAT', 'SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI'],
      datasets: [
        {
          data: [4, 3, 16, 12, 10, 14, 8],
          backgroundColor: ['#e0e3e5', '#e0e3e5', '#2563eb', '#2563eb', '#2563eb', '#2563eb', '#2563eb'],
          borderRadius: 4,
          borderSkipped: false
        }
      ]
    };
  }
}
