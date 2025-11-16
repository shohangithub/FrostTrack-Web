import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
  FormsModule,
} from '@angular/forms';
import { RoleService } from '../services/role-service.service';
import { ToastrModule, ToastrService } from 'ngx-toastr';
import {
  NgxDatatableModule,
  DatatableComponent,
} from '@swimlane/ngx-datatable';
import Swal from 'sweetalert2';
import { SwalConfirm } from 'app/theme-config';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { SYSTEM_ROLES } from 'app/common/data/settings-data';

@Component({
  selector: 'app-role',
  templateUrl: './role.component.html',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    NgxDatatableModule,
    ToastrModule,
  ],
})
export class RoleComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;

  rows: any[] = [];
  filteredData: any[] = [];
  loadingIndicator = false;
  form: FormGroup;
  editForm: FormGroup;
  selected: any[] = [];
  selection: any = 'checkbox';
  isRowSelected = false;
  roles = SYSTEM_ROLES;

  constructor(
    private roleService: RoleService,
    private fb: FormBuilder,
    private toastr: ToastrService,
    private modalService: NgbModal
  ) {
    this.form = this.fb.group({ name: ['', Validators.required] });
    this.editForm = this.fb.group({
      id: [''],
      name: ['', Validators.required],
    });
  }

  ngOnInit(): void {
    this.fetchData();
  }

  addRow(content: any) {
    this.modalService.open(content, {
      ariaLabelledBy: 'modal-basic-title',
      size: 'md',
    });
  }

  // selection handler for checkboxable rows
  onSelect({ selected }: { selected: any }) {
    this.selected.splice(0, this.selected.length);
    this.selected.push(...selected);
    this.isRowSelected = this.selected.length > 0;
  }

  // bulk delete selected roles
  deleteSelected() {
    const count = this.selected.length;
    if (count === 0) return;
    Swal.fire({
      title: 'Are you sure?',
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        const names = this.selected.map((x) => x.name ?? x.value ?? x);
        // call batch delete endpoint
        this.roleService.batchDelete(names).subscribe({
          next: () => {
            this.toastr.success(count + ' Roles Deleted Successfully');
            // remove locally
            names.forEach((n: any) => this.removeRecordByName(n));
            this.selected = [];
            this.isRowSelected = false;
          },
          error: () => this.toastr.error('Failed to delete roles'),
        });
      }
    });
  }

  private removeRecordByName(name: any) {
    this.rows = this.rows.filter((r) => (r.name ?? r.value ?? r) !== name);
  }

  fetchData() {
    this.loadingIndicator = true;
    this.roleService.list().subscribe({
      next: (r) => {
        this.rows = (r as any) || [];
        this.filteredData = JSON.parse(JSON.stringify(this.rows));
        this.loadingIndicator = false;
      },
      error: () => {
        this.loadingIndicator = false;
        this.toastr.error('Failed to load roles');
      },
    });
  }

  filterDatatable(event: any) {
    const val = (event.target.value || '').toLowerCase();
    // filter on role name/value/string
    const filtered = this.filteredData.filter((d: any) => {
      const name = (d.name ?? d.value ?? d).toString().toLowerCase();
      return name.indexOf(val) !== -1 || !val;
    });
    this.rows = filtered;
  }

  create() {
    if (this.form.invalid) return;
    const name = this.form.value.name.trim();
    if (!name) return;
    this.roleService.post({ name: name }).subscribe({
      next: () => {
        this.toastr.success('Role created');
        this.form.reset();
        this.modalService.dismissAll();
        this.fetchData();
      },
      error: () => this.toastr.error('Failed to create role'),
    });
  }

  // open edit modal and populate
  editRow(row: any, rowIndex: number, content: any) {
    this.modalService.open(content, {
      ariaLabelledBy: 'modal-basic-title',
      size: 'md',
    });
    const roleName = row.name ?? row.value ?? row;
    this.editForm.setValue({ id: row.id ?? null, name: roleName });
  }

  update() {
    if (this.editForm.invalid) return;
    const id = this.editForm.value.id;
    const name = this.editForm.value.name?.trim();
    if (!name) return;
    this.roleService.put(id, { name: name }).subscribe({
      next: () => {
        this.toastr.success('Role updated');
        this.modalService.dismissAll();
        this.fetchData();
      },
      error: () => this.toastr.error('Failed to update role'),
    });
  }

  delete(row: any) {
    const roleName = row.name ?? row.value ?? row;
    if (!roleName) return;
    Swal.fire({
      title: 'Are you sure?',
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        this.roleService.delete(roleName).subscribe({
          next: () => {
            this.toastr.success('Role deleted');
            this.fetchData();
          },
          error: () => this.toastr.error('Failed to delete role'),
        });
      }
    });
  }
}
