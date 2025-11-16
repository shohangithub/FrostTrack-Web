import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormsModule,
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { UserService } from '../services/user-service.service';
import { RoleService } from '../services/role-service.service';
import { IUserResponse } from '../models/user.interface';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-assign-role',
  templateUrl: './assign-role.component.html',
  styleUrls: ['./assign-role.component.sass'],
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
})
export class AssignRoleComponent implements OnInit {
  users: IUserResponse[] = [];
  roles: any[] = [];
  form: UntypedFormGroup;
  selectedUserRoles: string[] = [];

  constructor(
    private userService: UserService,
    private roleService: RoleService,
    private fb: UntypedFormBuilder,
    private toastr: ToastrService
  ) {
    this.form = this.fb.group({
      userId: ['', Validators.required],
      role: ['', Validators.required],
    });
  }

  ngOnInit(): void {
    this.loadUsers();
    this.loadRoles();
  }

  loadUsers() {
    this.userService
      .getWithPagination({ pageIndex: 0, pageSize: 1000 })
      .subscribe({
        next: (r) => (this.users = r.data as any),
        error: () => this.toastr.error('Failed to load users'),
      });
  }

  loadRoles() {
    this.roleService.list().subscribe({
      next: (r) => (this.roles = r as any),
      error: () => this.toastr.error('Failed to load roles'),
    });
  }

  onUserChange(userId: number) {
    if (!userId) {
      this.selectedUserRoles = [];
      return;
    }
    this.userService.getById(userId).subscribe({
      next: (u) =>
        (this.selectedUserRoles = u
          ? (u as any).roles ?? (u as any).Roles ?? []
          : []),
      error: () => (this.selectedUserRoles = []),
    });
  }

  assign() {
    const userId = this.form.value.userId;
    const role = this.form.value.role;
    this.userService.postRole(userId, role).subscribe({
      next: () => {
        this.toastr.success('Role assigned');
        this.form.reset();
        this.onUserChange(userId);
      },
      error: () => this.toastr.error('Failed to assign role'),
    });
  }
}
