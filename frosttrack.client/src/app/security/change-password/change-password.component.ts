import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { UserService } from '../services/user-service.service';
import {
  IChangePasswordRequest,
  ISetPasswordRequest,
} from '../models/user.interface';
import { AuthService } from '@core/service/auth.service';
import { LayoutService } from '@core/service/layout.service';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './change-password.component.html',
})
export class ChangePasswordComponent implements OnInit {
  passwordForm!: FormGroup;
  isSubmitting = false;
  isAdminMode = false;
  userId!: number;
  userName: string = '';

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private authService: AuthService,
    private toastr: ToastrService,
    private router: Router,
    private route: ActivatedRoute,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit(): void {
    // Check if userId is passed in query params (admin setting password for user)
    this.route.queryParams.subscribe((params) => {
      if (params['userId'] && params['userName']) {
        this.isAdminMode = true;
        this.userId = +params['userId'];
        this.userName = params['userName'];
        this.initSetPasswordForm();
      } else {
        this.isAdminMode = false;
        this.initChangePasswordForm();
      }
    });
  }

  initChangePasswordForm(): void {
    this.passwordForm = this.fb.group(
      {
        currentPassword: ['', [Validators.required, Validators.minLength(6)]],
        newPassword: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', [Validators.required]],
      },
      { validators: this.passwordMatchValidator }
    );
  }

  initSetPasswordForm(): void {
    this.passwordForm = this.fb.group(
      {
        newPassword: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', [Validators.required]],
      },
      { validators: this.passwordMatchValidator }
    );
  }

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const newPassword = control.get('newPassword');
    const confirmPassword = control.get('confirmPassword');

    if (!newPassword || !confirmPassword) {
      return null;
    }

    return newPassword.value === confirmPassword.value
      ? null
      : { passwordMismatch: true };
  }

  onSubmit(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      this.toastr.error('Please fill all required fields correctly');
      return;
    }

    this.isSubmitting = true;

    if (this.isAdminMode) {
      // Admin setting password for another user
      const payload: ISetPasswordRequest = {
        userId: this.userId,
        newPassword: this.passwordForm.value.newPassword,
        confirmPassword: this.passwordForm.value.confirmPassword,
      };

      this.userService.setPassword(payload).subscribe({
        next: () => {
          this.router.navigate(['/security/user']);
        },
        error: () => {
          this.isSubmitting = false;
        },
      });
    } else {
      // User changing their own password
      // Get current user ID from auth service
      const userId = this.authService.getUserId();
      if (!userId) {
        this.toastr.error('User not authenticated');
        this.isSubmitting = false;
        return;
      }

      const payload: IChangePasswordRequest = {
        currentPassword: this.passwordForm.value.currentPassword,
        newPassword: this.passwordForm.value.newPassword,
        confirmPassword: this.passwordForm.value.confirmPassword,
      };

      this.userService.changePassword(userId, payload).subscribe({
        next: () => {
          this.router.navigate(['/dashboard/main']);
        },
        error: () => {
          this.isSubmitting = false;
        },
      });
    }
  }

  cancel(): void {
    if (this.isAdminMode) {
      this.router.navigate(['/security/user']);
    } else {
      this.router.navigate(['/dashboard/main']);
    }
  }
}
