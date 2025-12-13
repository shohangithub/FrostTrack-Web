import { Component, OnInit, ViewChild } from '@angular/core';
import {
  DatatableComponent,
  SelectionType,
  NgxDatatableModule,
} from '@swimlane/ngx-datatable';
import {
  UntypedFormGroup,
  UntypedFormBuilder,
  UntypedFormControl,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrModule, ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { Router } from '@angular/router';
import {
  IUserListResponse,
  IUserRequest,
  IUserResponse,
} from '../models/user.interface';
import { UserService } from '../services/user-service.service';
import { CommonModule } from '@angular/common';
import {
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';
import { SwalConfirm } from 'app/theme-config';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  PaginationResult,
  PagingResponse,
} from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  COMMON_STATUS_LIST,
  SYSTEM_ROLES,
  ROLE_HIERARCHY,
} from 'app/common/data/settings-data';
import { LayoutService } from '@core/service/layout.service';
import { AuthService } from '@core/service/auth.service';
import { environment } from 'environments/environment.development';

@Component({
  selector: 'app-user',
  templateUrl: './user.component.html',
  styleUrls: ['./user.component.sass'],
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
  ],
  providers: [UserService],
})
export class UserComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;
  rows = [];
  scrollBarHorizontal = window.innerWidth < 1200;
  selectedRowData!: IUserResponse;
  data: IUserListResponse[] = [];
  newUserImg = 'assets/images/users/user-2.png';
  selectedFile: File | null = null;
  imagePreview: string | null = null;
  uploadingImage = false;
  serverUrl = environment.resource_Url;
  filteredData: any[] = [];
  editForm: UntypedFormGroup;
  register!: UntypedFormGroup;
  loadingIndicator = true;
  isRowSelected = false;
  selectedOption!: string;
  reorderable = true;
  selected: IUserListResponse[] = [];
  pagination: PaginationQuery = { pageSize: 10, pageIndex: 0 };
  paging: PagingResponse | undefined;

  roles = SYSTEM_ROLES;
  statusList = COMMON_STATUS_LIST;
  currentUserRole: string | null = null;
  availableRoles: typeof SYSTEM_ROLES = [];
  MessageHub = {
    ADD: 'Add Record Successfully',
    UPDATE: '',
    DELETE_CONFIRM: 'Are you sure?',
    DELETE: '',
  };

  @ViewChild(DatatableComponent, { static: false }) table2!: DatatableComponent;
  selection!: SelectionType;
  constructor(
    private fb: UntypedFormBuilder,
    private modalService: NgbModal,
    private toastr: ToastrService,
    private userService: UserService,
    private layoutService: LayoutService,
    private authService: AuthService,
    private router: Router
  ) {
    this.editForm = this.fb.group({
      id: new UntypedFormControl(),
      userName: new UntypedFormControl('', [
        Validators.required,
        Validators.pattern('^[a-zA-Z ]+$'),
      ]),
      email: new UntypedFormControl('', [
        Validators.required,
        Validators.email,
        Validators.minLength(5),
      ]),
      isActive: new UntypedFormControl('', [Validators.required]),
      role: new UntypedFormControl('', [Validators.required]),
      profileImageUrl: new UntypedFormControl(''),
    });

    window.onresize = () => {
      this.scrollBarHorizontal = window.innerWidth < 1200;
    };
    this.selection = SelectionType.checkbox;
    this.layoutService.loadCurrentRoute();
  }

  // select record using check box
  onSelect({ selected }: { selected: any }) {
    this.selected.splice(0, this.selected.length);
    this.selected.push(...selected);

    if (this.selected.length === 0) {
      this.isRowSelected = false;
    } else {
      this.isRowSelected = true;
    }
  }

  deleteSelected() {
    // Filter selected users to only include those with accessible roles
    const accessibleUsers = this.selected.filter((user) =>
      this.canAccessUser(user.roleNames[0])
    );

    if (accessibleUsers.length === 0) {
      this.toastr.error(
        'You do not have permission to delete the selected user(s)'
      );
      return;
    }

    if (accessibleUsers.length < this.selected.length) {
      this.toastr.warning(
        'Some users were excluded due to insufficient permissions'
      );
    }

    Swal.fire({
      title: this.MessageHub.DELETE_CONFIRM,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        const ids = accessibleUsers.map((x) => x.id);
        this.userService.batchDelete(ids).subscribe({
          next: (response) => {
            if (response) {
              accessibleUsers.forEach((row) => {
                this.removeRecord(row);
              });
              this.deleteRecordSuccess(accessibleUsers.length);
              this.selected = [];
              this.isRowSelected = false;
            }
          },
          error: (err: ErrorResponse) => {
            this.toastr.error(formatErrorMessage(err));
          },
        });
      }
    });
  }

  ngOnInit() {
    this.getCurrentUserRole();
    this.filterAvailableRoles();
    this.fetchData();

    //subject call change open text search
    this.searchSubject
      .pipe(debounceTime(1000), distinctUntilChanged())
      .subscribe((value: any) => {
        this.pagination.openText = value;
        this.fetchData();
      });

    this.register = this.fb.group({
      userName: ['', [Validators.required, Validators.pattern('^[a-zA-Z ]+$')]],
      role: ['', [Validators.required]],
      email: [
        '',
        [Validators.required, Validators.email, Validators.minLength(5)],
      ],
      isActive: ['', [Validators.required]],
      profileImageUrl: [''],
    });
  }

  getCurrentUserRole(): void {
    const roles = this.authService.getUserRoles();
    // Handle both single role string and array of roles
    this.currentUserRole = Array.isArray(roles) ? roles[0] : roles;
  }

  filterAvailableRoles(): void {
    if (!this.currentUserRole) {
      // If no role found, show all roles (fallback for development)
      this.availableRoles = SYSTEM_ROLES;
      return;
    }

    const allowedRoleIds = ROLE_HIERARCHY[this.currentUserRole] || [];

    // Filter SYSTEM_ROLES to only include roles that current user can create
    this.availableRoles = SYSTEM_ROLES.filter((role) =>
      allowedRoleIds.includes(role.id)
    );

    // If no roles available (shouldn't happen), default to current role only
    if (this.availableRoles.length === 0) {
      this.availableRoles = SYSTEM_ROLES.filter(
        (role) => role.id === this.currentUserRole
      );
    }
  }

  canAccessUser(userRole: string): boolean {
    // If no current user role, allow access (fallback for development)
    if (!this.currentUserRole) {
      return true;
    }

    // Get allowed roles for current user
    const allowedRoleIds = ROLE_HIERARCHY[this.currentUserRole] || [];

    // Check if the user's role is in the allowed list
    return allowedRoleIds.includes(userRole);
  }

  filterUsersByRole(users: IUserListResponse[]): IUserListResponse[] {
    // Filter users to only show those with accessible roles
    return users.filter((user) => this.canAccessUser(user.roleNames[0]));
  }

  fetchData() {
    this.userService.getWithPagination(this.pagination).subscribe({
      next: (response: PaginationResult<IUserListResponse>) => {
        // Filter users based on role hierarchy
        this.data = this.filterUsersByRole(response.data);
        this.paging = response.paging;
        this.loadingIndicator = false;
      },
      error: (err: ErrorResponse) => {
        this.loadingIndicator = false;
        const errString = formatErrorMessage(err);
        this.toastr.error(errString);
      },
    });
  }

  changePagination(pageInfo: any) {
    this.pagination.pageIndex = pageInfo.offset;
    this.fetchData();
  }

  //on sorting
  onSortring(event: any) {
    const sort = event.sorts[0];
    this.pagination.orderBy = sort.prop;
    this.pagination.isAscending = sort.dir === 'desc' ? false : true;
    this.fetchData();
  }

  // add new record
  addRow(content: any) {
    // Reset form and image upload state
    this.register.reset();
    this.resetImageUpload();

    this.modalService.open(content, {
      ariaLabelledBy: 'modal-basic-title',
      size: 'lg',
    });
  }
  // edit record
  editRow(row: any, rowIndex: number, content: any) {
    // Check if current user can access this user's role
    if (!this.canAccessUser(row.roleNames[0])) {
      this.toastr.error('You do not have permission to edit this user');
      return;
    }

    // Reset image upload state
    this.resetImageUpload();

    this.modalService.open(content, {
      ariaLabelledBy: 'modal-basic-title',
      size: 'lg',
    });

    this.userService.getById(row.id).subscribe({
      next: (response: IUserResponse) => {
        this.editForm.setValue({
          id: response.id,
          userName: response.userName,
          email: response.email,
          role:
            response.roleNames && response.roleNames.length > 0
              ? response.roleNames[0]
              : '',
          isActive: response.isActive || false,
          profileImageUrl: response.profileImageUrl || '',
        });
        this.loadingIndicator = false;
      },
      error: (err) => {
        console.log(err);
        this.loadingIndicator = false;
      },
    });

    this.selectedRowData = row;
  }
  // save add new record
  save(form: UntypedFormGroup) {
    if (this.register.valid) {
      const payload: IUserRequest = { ...form.value };
      payload.profileImageUrl = payload.profileImageUrl
        ? this.serverUrl + payload.profileImageUrl
        : '';
      this.userService.create(payload).subscribe({
        next: (response: IUserResponse) => {
          const mappedData: IUserListResponse = {
            id: response.id,
            userName: response.userName,
            email: response.email,
            roleNames:
              response.roleNames && response.roleNames.length > 0
                ? response.roleNames
                : [],
            status: response.status,
          };
          const temp = JSON.parse(JSON.stringify(this.data));
          temp.push(mappedData);
          this.data.length = 0;
          this.data = temp;
          console.log(this.data);
          form.reset();
          this.modalService.dismissAll();
          this.fetchData();
        },
        error: (err: ErrorResponse) => {
          const errString = formatErrorMessage(err);
          this.toastr.error(errString);
        },
      });
    }
  }
  // save record on edit
  edit(form: UntypedFormGroup) {
    if (this.editForm.valid) {
      const formData = form.value;
      const payload: IUserRequest = { ...formData };
      if (!payload.profileImageUrl?.startsWith(this.serverUrl)) {
        payload.profileImageUrl = payload.profileImageUrl
          ? this.serverUrl + payload.profileImageUrl
          : '';
      }

      this.userService.update(formData.id, payload).subscribe({
        next: (response: IUserResponse) => {
          this.data = this.data.filter((value, key) => {
            if (value.id == response.id) {
              value.userName = response.userName;
              value.email = response.email;
              value.status = response.status;
              value.roleNames =
                response.roleNames && response.roleNames.length > 0
                  ? response.roleNames
                  : [];
            }
            this.modalService.dismissAll();
            return true;
          });
          form.reset();
          this.modalService.dismissAll();
        },
        error: (err: ErrorResponse) => {
          this.toastr.error(formatErrorMessage(err));
        },
      });
    }
  }

  // delete single row
  delete(row: any) {
    // Check if current user can access this user's role
    if (!this.canAccessUser(row.roleNames[0])) {
      this.toastr.error('You do not have permission to delete this user');
      return;
    }

    Swal.fire({
      title: this.MessageHub.DELETE_CONFIRM,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        this.userService.remove(row.id).subscribe({
          next: (response) => {
            if (response) {
              this.removeRecord(row);
            }
          },
          error: (err: ErrorResponse) => {
            this.toastr.error(formatErrorMessage(err));
          },
        });
      }
    });
  }

  private removeRecord(row: any) {
    this.data = this.arrayRemove(this.data, row.id);
  }
  private arrayRemove(array: any[], id: any) {
    return array.filter(function (element) {
      return element.id !== id;
    });
  }

  // filter table data
  /*  filterDatatable(event: any) {
      // get the value of the key pressed and make it lowercase
      const val = event.target.value.toLowerCase();
      // get the amount of columns in the table
      const colsAmt = this.columns.length;
      // get the key names of each column in the dataset
      const keys = Object.keys(this.filteredData[0]);
      // assign filtered matches to the active datatable
  
      this.data = this.filteredData.filter((item) => {
        // iterate through each row's column data
        for (let i = 0; i < colsAmt; i++) {
          // check for a match
          if (
            item[keys[i]].toString().toLowerCase().indexOf(val) !== -1 ||
            !val
          ) {
            // found match, return true to add to result set
            return true;
          }
        }
        return false;
      });
      // whenever the filter changes, always go back to the first page
      this.table.offset = 0;
    }*/

  searchSubject = new Subject<any>();
  filterDatatable(event: any) {
    const val = event.target.value.toLowerCase();
    this.searchSubject.next(val);
  }

  // get random id
  getId(min: number, max: number) {
    // min and max included
    return Math.floor(Math.random() * (max - min + 1) + min);
  }

  addRecordSuccess() {
    // Success message is now handled by the service
  }
  updateRecordSuccess() {
    // Success message is now handled by the service
  }

  deleteRecordSuccess(count: number) {
    // Success message is now handled by the service
  }

  changePassword(row: IUserListResponse) {
    this.router.navigate(['/security/change-password'], {
      queryParams: { userId: row.id, userName: row.userName },
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];

      // Validate file type
      if (!file.type.startsWith('image/')) {
        this.toastr.error('Please select an image file');
        return;
      }

      // Validate file size (max 5MB)
      if (file.size > 5 * 1024 * 1024) {
        this.toastr.error('Image size must not exceed 5MB');
        return;
      }

      this.selectedFile = file;

      // Create image preview
      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.imagePreview = e.target.result;
      };
      reader.readAsDataURL(file);

      // Upload image
      this.uploadImage();
    }
  }

  uploadImage(): void {
    if (!this.selectedFile) return;

    this.uploadingImage = true;
    this.userService.uploadProfileImage(this.selectedFile).subscribe({
      next: (response) => {
        this.register.patchValue({ profileImageUrl: response.imageUrl });
        this.editForm.patchValue({ profileImageUrl: response.imageUrl });
        this.uploadingImage = false;
      },
      error: () => {
        this.uploadingImage = false;
        this.selectedFile = null;
        this.imagePreview = null;
      },
    });
  }

  resetImageUpload(): void {
    this.selectedFile = null;
    this.imagePreview = null;
    this.register.patchValue({ profileImageUrl: '' });
    this.editForm.patchValue({ profileImageUrl: '' });
  }
}
