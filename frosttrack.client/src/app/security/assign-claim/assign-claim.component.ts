import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormsModule,
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import {
  NgxDatatableModule,
  DatatableComponent,
} from '@swimlane/ngx-datatable';
import { UserService } from '../services/user-service.service';
import { AssignClaimService } from '../services/assign-claim-service.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-assign-claim',
  templateUrl: './assign-claim.component.html',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, NgxDatatableModule],
})
export class AssignClaimComponent implements OnInit {
  users: any[] = [];
  usersWithClaims: Array<{
    id: number;
    userName: string;
    email: string;
    claims: Array<{ type: string; value: string }>;
  }> = [];
  // Track which user rows are expanded to show claim details
  expandedUserIds: Set<number> = new Set<number>();

  @ViewChild('claimsTable') claimsTable?: DatatableComponent;
  form: UntypedFormGroup;
  selectedUserClaims: Array<{ key: string; value: string }> = [];
  selectedUserRoles: string[] = [];

  constructor(
    private userService: UserService,
    private assignClaimService: AssignClaimService,
    private fb: UntypedFormBuilder,
    private toastr: ToastrService
  ) {
    this.form = this.fb.group({
      userId: ['', Validators.required],
      claimKey: ['', Validators.required],
      claimValue: ['', Validators.required],
    });
  }

  ngOnInit(): void {
    this.loadUsers();
    this.loadUsersWithClaims();
  }

  // Toggle showing claim details for a user row
  toggleUserClaims(userId: number) {
    if (!userId && userId !== 0) return;
    if (this.expandedUserIds.has(userId)) {
      this.expandedUserIds.delete(userId);
    } else {
      this.expandedUserIds.add(userId);
    }
  }

  isExpanded(userId: number) {
    return this.expandedUserIds.has(userId);
  }

  // Toggle using ngx-datatable row detail API as well as local state
  toggleRow(row: any) {
    if (!row) return;
    const id = row.id;
    this.toggleUserClaims(id);
    try {
      this.claimsTable?.rowDetail.toggleExpandRow(row);
    } catch (e) {
      // ignore if rowDetail not available yet
    }
  }

  // Handle ngx-datatable activation events (clicks). Activate event payload: { type, row }
  onActivate(event: any) {
    try {
      if (event && event.type === 'click' && event.row) {
        this.toggleRow(event.row);
      }
    } catch (e) {
      // noop
    }
  }

  loadUsers() {
    this.userService
      .getWithPagination({ pageIndex: 0, pageSize: 1000 })
      .subscribe({
        next: (r) => (this.users = r.data as any),
        error: () => {},
      });
  }

  onUserChange(userId: number) {
    if (!userId) {
      this.selectedUserClaims = [];
      this.selectedUserRoles = [];
      return;
    }
    this.userService.getById(userId).subscribe({
      next: (u) => {
        const user = u as any;
        this.selectedUserClaims = (user.claims ?? []).map((c: any) => ({
          key: c.type ?? c.key,
          value: c.value ?? c.value,
        }));
        this.selectedUserRoles = user.roles ?? user.Roles ?? [];
      },
      error: () => {
        this.selectedUserClaims = [];
        this.selectedUserRoles = [];
      },
    });
  }

  addClaim() {
    const userId = this.form.value.userId;
    const key = this.form.value.claimKey;
    const value = this.form.value.claimValue;
    this.assignClaimService.addClaim(userId, { key, value }).subscribe({
      next: () => {
        this.toastr.success('Claim added');
        this.form.patchValue({ claimKey: '', claimValue: '' });
        this.onUserChange(userId);
        this.loadUsersWithClaims();
      },
      error: () => this.toastr.error('Failed to add claim'),
    });
  }

  removeClaim() {
    const userId = this.form.value.userId;
    const key = this.form.value.claimKey;
    const value = this.form.value.claimValue;
    this.assignClaimService.removeClaim(userId, { key, value }).subscribe({
      next: () => {
        this.toastr.success('Claim removed');
        this.form.patchValue({ claimKey: '', claimValue: '' });
        this.onUserChange(userId);
      },
      error: () => this.toastr.error('Failed to remove claim'),
    });
  }

  loadUsersWithClaims() {
    this.assignClaimService.getAllUserClaims().subscribe({
      next: (r) => (this.usersWithClaims = r || []),
      error: () => this.toastr.error('Failed to load user claims'),
    });
  }
}
