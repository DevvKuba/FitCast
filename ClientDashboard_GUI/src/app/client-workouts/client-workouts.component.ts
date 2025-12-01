import { Component, inject, resolveForwardRef, ViewChild} from '@angular/core';
import { TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { Workout } from '../models/workout';
import { WorkoutService } from '../services/workout.service';
import { SpinnerComponent } from "../spinner/spinner.component";
import { SelectModule } from 'primeng/select';
import { AccountService } from '../services/account.service';
import { UserDto } from '../models/dtos/user-dto';
import { Toast } from 'primeng/toast';
import { InputTextModule } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { FormsModule } from '@angular/forms';
import { AutoCompleteCompleteEvent, AutoCompleteModule } from 'primeng/autocomplete';
import { Client } from '../models/client';
import { ClientService } from '../services/client.service';
import { DatePicker } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageService } from 'primeng/api';
import { ToastService } from '../services/toast.service';
import { TagModule } from 'primeng/tag';
import { ToggleButton, ToggleButtonModule } from 'primeng/togglebutton';
import { PasswordModule } from 'primeng/password';
import { TrainerService } from '../services/trainer.service';
import { Popover, PopoverModule } from 'primeng/popover';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';

@Component({
  selector: 'app-client-workouts',
  imports: [TableModule, CommonModule, ButtonModule, SpinnerComponent, Toast, InputTextModule,
     Dialog, FormsModule, AutoCompleteModule, DatePicker, InputNumberModule, TagModule, SelectModule,
     ToggleButtonModule, ToggleButton, PasswordModule, PopoverModule, IconFieldModule, InputIconModule ],
  templateUrl: './client-workouts.component.html',
  providers: [MessageService],
  styleUrl: './client-workouts.component.css'
})
export class ClientWorkouts {
    @ViewChild('op') op!: Popover;
    workouts: Workout[] | null = null;
    trainerId : number  = 0;
    autoWorkoutRetrievalVisible: boolean = false;
    addDialogVisible: boolean = false;
    deleteDialogVisible: boolean = false;

    selectedClient :{id: number, name: string} = {id: 0, name: ""};
    workoutTitle: string = "";
    sessionDate: Date  = new Date();
    exerciseCount: number = 0;
    currentUserId: number = 0;
    clients: {id: number, name: string}[] = [];
    clonedWorkouts: { [s: string]: Workout } = {};
    deleteWorkoutId: number = 0;
    deleteWorkoutTitle: string = "";
    trainerApiKey: string = "";
    retrievalInfoText: string = "";
    automaticRetrievalChecked: boolean = false;
    validApiKeyProvided: boolean = false;

    private workoutService = inject(WorkoutService);
    private accountService = inject(AccountService);
    private clientService = inject(ClientService);
    private trainerService = inject(TrainerService);
    private toastService = inject(ToastService);

    first = 0; // offset
    rows = 10; // pageSize

    ngOnInit() {
        this.currentUserId = this.accountService.currentUser()?.id ?? 0;
        this.displayWorkouts();
        this.getAutoWorkoutRetrievalStatus();
        this.setRetrievalInfoText();
        this.getTrainerApiKey();
        if(this.trainerApiKey !== null){
            this.validApiKeyProvided = true;
        }
    }

    next() {
        this.first = this.first + this.rows;
    }

    prev() {
        this.first = this.first - this.rows;
    }

    reset() {
        this.first = 0;
    }

    pageChange(event: { first: number; rows: number; }) {
        this.first = event.first;
        this.rows = event.rows;
    }

    isLastPage(): boolean {
        return this.workouts ? this.first + this.rows >= this.workouts.length : true;
    }

    isFirstPage(): boolean {
        return this.workouts ? this.first === 0 : true;
    }

     toggle(event: any) {
        this.op.toggle(event);
    }

    setRetrievalInfoText(){
        this.retrievalInfoText = "You can configure your Hevy Workout Api for either manual or automated" +
        " workout collection, the automatic background retrieval process occurs daily at midnight"
    }

    getTrainerApiKey(){
        this.trainerService.getWorkoutRetrievalApiKey(this.currentUserId).subscribe({
            next: (response) => {
                this.trainerApiKey = response.data;
            }
        })
    }

    getAutoWorkoutRetrievalStatus(){
        this.trainerService.getAutoWorkoutRetrievalStatus(this.currentUserId).subscribe({
            next: (response) => {
                this.automaticRetrievalChecked = response.data;
            }
        })
    }

    updateWorkoutRetrievalDetails(apiKey: string, retrievalStatus: boolean){
        this.trainerService.updateTrainerRetrievalDetails(this.currentUserId,
        apiKey, retrievalStatus).subscribe({
            next: (response) => {
                this.toastService.showSuccess('Success updated details', response.message);
                this.autoWorkoutRetrievalVisible = false
            }, 
            error: (response) => {
                this.toastService.showError('Error updating details', response.error.message);
            }
        })
    }

     onRowEditInit(workout: Workout) {
        this.clonedWorkouts[workout.id as number] = { ...workout };
    }

    onRowEditSave(newWorkout: Workout) {
        console.log(typeof(newWorkout.sessionDate));
        if (newWorkout.workoutTitle.length !== 0 && newWorkout.sessionDate !== null 
            && newWorkout.exerciseCount > 0 && newWorkout.id !== null) {
            delete this.clonedWorkouts[newWorkout.id as number];

            var updatedInfo =  {
                id: newWorkout.id,
                workoutTitle: newWorkout.workoutTitle,
                sessionDate: newWorkout.sessionDate,
                exerciseCount: newWorkout.exerciseCount
            }
            this.workoutService.updateWorkout(updatedInfo).subscribe({
                next: (response) => {
                    console.log(response);
                    this.toastService.showSuccess('Updated Correctly', `Successfully updated ${newWorkout.clientName}'s workout details`);
                }, 
                error: (response) => {
                    console.log(response);
                    this.toastService.showError('Unsuccessful Update', `Workout: ${newWorkout.workoutTitle} not updated`);
                }
            })
        } else {
            this.toastService.showError('Update Unsuccessful', 'Ensure all fields are filled in correctly');
        }
    }

    onRowEditCancel(workout: Workout, index: number) {
        if(this.workouts && this.clonedWorkouts[workout.id as number]){
            this.workouts[index] = this.clonedWorkouts[workout.id as number];
            delete this.clonedWorkouts[workout.id as number];
        }
    }

    onRowDelete(workoutId: number){
        this.workoutService.deleteWorkout(workoutId).subscribe({
            next: (response) => {
                this.toastService.showSuccess('Success Deleting Workout', response.message);
                this.deleteDialogVisible = false;
                this.displayWorkouts();
            },
            error: (response) => {
                console.log(response);
                this.toastService.showError('Unsuccessful Workout Deletion', response.error.message);
            }
        })
    }

    addNewWorkout(selectedClient : {id: number, name: string}, workoutTitle: string, sessionDate : Date | undefined, exerciseCount: number){
        var newWorkout = {
            workoutTitle: workoutTitle,
            clientName: selectedClient.name,
            clientId: selectedClient.id,
            sessionDate: this.formatDateForApi(sessionDate),
            exerciseCount: exerciseCount
        }

        this.workoutService.addWorkout(newWorkout).subscribe({
            next: (response) => {
                this.addDialogVisible = false;
                this.toastService.showSuccess('Successfully added workout', response.message);
                this.displayWorkouts();
            },
            error: (response) => {
                this.toastService.showError('Workout not added', response.error.message);
            }
        })
    }

    displayWorkouts(){
        this.trainerId = this.currentUserId;
        console.log(this.trainerId);
        this.workoutService.retrieveTrainerClientWorkouts(this.trainerId).subscribe({
            next: (response) => {
                this.workouts = response.data ?? [];
            },
            error: (response) => {
                console.log(response.error.message)
            }
        });
    }

  gatherExternalWorkouts(){
    this.trainerService.gatherAndUpdateExternalWorkouts(this.currentUserId).subscribe({
        next: (response) => {
            if(response.data == 0){
                this.toastService.showNeutral('Success Gathering Workouts', `Accessed workouts successfully, No workouts found to retrieve`);
            }else {
                this.toastService.showSuccess('Success Gathering Workouts', `All ${response.data} workouts have been added and client details updated`);
            }
            this.displayWorkouts();
        },
        error: () => {
            this.toastService.showError('Error Gathering Workouts', 'An error occurred calling the external api');
        }
    })
  }

  showDialogForAutoWorkoutRetrieval(){
    this.autoWorkoutRetrievalVisible = true;
  }

  showDialogForAdd() {
    this.gatherClientNames();
    this.addDialogVisible = true;
    }

  showDialogForDelete(workoutId: number, workoutTitle : string){
    this.deleteDialogVisible = true;
    this.deleteWorkoutId = workoutId;
    this.deleteWorkoutTitle = workoutTitle;
  }


  formatDateForApi(date: Date | undefined): string {
  if (!date) return '';
  
  const day = date.getDate().toString().padStart(2, '0');
  const month = (date.getMonth() + 1).toString().padStart(2, '0');
  const year = date.getFullYear();
  
  return `${day}/${month}/${year}`;
    }

  gatherClientNames(){
    this.clientService.gatherClientNames(this.currentUserId).subscribe({
        next: (response) => {
            this.clients = response
        }
    });
    }
}
