import { Component, OnInit } from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import {
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { FeatherModule } from 'angular-feather';
import { AuthService } from '@core';
import { LoginRequest } from '@core/models/login-request';
import { TokenResponse } from '@core/models/token-response';
import {
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';
import { ToastrService } from 'ngx-toastr';
import { JwtHelperService } from 'angular-jwt-updated';

@Component({
  selector: 'app-signin',
  templateUrl: './signin.component.html',
  styleUrls: ['./signin.component.scss'],
  standalone: true,
  imports: [FormsModule, ReactiveFormsModule, FeatherModule, RouterLink],
})
export class SigninComponent implements OnInit {
  loginForm!: UntypedFormGroup;
  submitted = false;
  returnUrl!: string;
  error = '';
  hide = true;
  private jwtHelper = new JwtHelperService();

  constructor(
    private formBuilder: UntypedFormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    // Check if user is already logged in with valid token
    const currentUser = this.authService.currentUserValue;
    if (currentUser && currentUser.token) {
      const isExpired = this.jwtHelper.isTokenExpired(currentUser.token);
      if (!isExpired) {
        // User already logged in, redirect to dashboard or returnUrl
        const returnUrl =
          this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
        this.router.navigate([returnUrl]);
        return;
      }
    }

    // Get return URL from route parameters or default to dashboard
    this.returnUrl =
      this.route.snapshot.queryParams['returnUrl'] || '/dashboard';

    // Check if redirected due to session expiry
    const reason = this.route.snapshot.queryParams['reason'];
    if (reason === 'expired') {
      this.toastr.warning(
        'Your session has expired. Please log in again.',
        'Session Expired',
        {
          timeOut: 5000,
          closeButton: true,
        }
      );
    }

    this.loginForm = this.formBuilder.group({
      username: ['ris.shohan@gmail.com', Validators.required],
      password: ['1qazZAQ!', Validators.required],
      remember: [''],
    });
  }
  get f() {
    return this.loginForm.controls;
  }
  onSubmit() {
    this.submitted = true;
    this.error = '';

    if (this.loginForm.invalid) {
      this.error = 'Username and Password not valid !';
      return;
    } else {
      const payload: LoginRequest = {
        email: this.f['username'].value,
        password: this.f['password'].value,
      };
      this.authService.post(payload).subscribe({
        next: (response: TokenResponse) => {
          if (response) {
            localStorage.setItem('currentUser', JSON.stringify(response));
            this.authService.currentUserSubject.next(response);
            if (response.token) {
              // Navigate to returnUrl or default dashboard
              this.router.navigate([this.returnUrl]);
            }
          } else {
            this.error = 'Invalid Login';
          }
        },
        error: (err: ErrorResponse) => {
          const errString = formatErrorMessage(err);
          this.toastr.error(errString);
        },
      });

      // this.authService
      //   .login(this.f['username'].value, this.f['password'].value)
      //   .subscribe({
      //     next: (res) => {
      //       if (res) {
      //         if (res) {
      //           const token = this.authService.currentUserValue.token;
      //           if (token) {
      //             this.router.navigate(['/dashboard/main']);
      //           }
      //         } else {
      //           this.error = 'Invalid Login';
      //         }
      //       } else {
      //         this.error = 'Invalid Login';
      //       }
      //     },
      //     error: (error) => {
      //       this.error = error;
      //       this.submitted = false;
      //     },
      //   });
    }
  }
}
