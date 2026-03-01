import { Component, Input, OnChanges, SimpleChanges, Inject, LOCALE_ID } from '@angular/core';
import { formatDate } from '@angular/common';
import { CustomFieldService, CustomFieldValueDto } from '../../core/services/custom-field.service';

@Component({
  selector: 'app-custom-fields',
  templateUrl: './custom-fields.component.html',
  styles: [`
    .custom-fields-loading { font-size: 0.75rem; color: #94a3b8; font-style: italic; margin-top: 0.5rem; }
    .custom-fields-error { font-size: 0.75rem; color: #ef4444; margin-top: 0.5rem; }
    .custom-fields { margin-top: 0.75rem; border-top: 1px dashed #e2e8f0; padding-top: 0.5rem; }
    .cf-row { display: flex; align-items: center; gap: 0.5rem; padding: 0.25rem 0; font-size: 0.8125rem; flex-wrap: wrap; }
    .cf-label { color: #64748b; font-weight: 500; min-width: 100px; }
    .cf-type { color: #cbd5e1; font-size: 0.6875rem; }
    .cf-value {
      color: #374151; cursor: pointer; padding: 0.125rem 0.375rem;
      border-radius: 4px; border: 1px dashed transparent; transition: border-color 0.15s;
    }
    .cf-value:hover { border-color: #cbd5e1; }
    .cf-required { color: #ef4444; font-weight: 600; }
    .cf-error { color: #ef4444; font-size: 0.75rem; width: 100%; margin-top: 0.125rem; }
    .cf-edit { display: flex; gap: 0.25rem; align-items: center; flex-wrap: wrap; }
    .cf-input {
      padding: 0.25rem 0.5rem; border: 1px solid #3b82f6; border-radius: 4px;
      font-size: 0.8125rem; min-width: 120px; outline: none;
    }
    .cf-input-error { border-color: #ef4444; }
    .cf-input:focus { box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.2); }
    .cf-input-error:focus { box-shadow: 0 0 0 2px rgba(239, 68, 68, 0.2); }
    .cf-textarea { min-height: 60px; resize: vertical; min-width: 200px; font-family: inherit; }
    select.cf-input { min-width: 140px; cursor: pointer; }
    .cf-toggle { display: flex; align-items: center; gap: 0.375rem; cursor: pointer; }
    .cf-toggle input[type="checkbox"] { width: 16px; height: 16px; cursor: pointer; }
    .cf-toggle-label { font-weight: 500; color: #374151; }
    .cf-multi-select { display: flex; gap: 0.5rem; flex-wrap: wrap; }
    .cf-multi-option { display: flex; align-items: center; gap: 0.25rem; font-size: 0.8125rem; cursor: pointer; }
    .cf-multi-option input[type="checkbox"] { width: 14px; height: 14px; cursor: pointer; }
    .cf-currency-wrap { display: flex; align-items: center; gap: 0; }
    .cf-currency-symbol {
      padding: 0.25rem 0.375rem; background: #f1f5f9; border: 1px solid #3b82f6;
      border-right: none; border-radius: 4px 0 0 4px; color: #64748b; font-weight: 600; font-size: 0.8125rem;
    }
    .cf-input-currency { border-radius: 0 4px 4px 0; }
    .cf-save, .cf-cancel {
      width: 24px; height: 24px; border: none; border-radius: 4px;
      display: flex; align-items: center; justify-content: center;
      font-size: 0.75rem; cursor: pointer; min-height: 24px; min-width: 24px;
    }
    .cf-save { background: #dcfce7; color: #166534; }
    .cf-save:hover { background: #bbf7d0; }
    .cf-cancel { background: #fee2e2; color: #991b1b; }
    .cf-cancel:hover { background: #fecaca; }
  `]
})
export class CustomFieldsComponent implements OnChanges {
  @Input() entityId = '';
  @Input() domainType = '';
  @Input() entityType = '';
  @Input() requiredFields: string[] = [];
  fields: CustomFieldValueDto[] = [];
  loading = false;
  loadError = '';
  editing: string | null = null;
  editValue = '';
  editError = '';
  private multiSelectValues: string[] = [];
  private baseFields: CustomFieldValueDto[] = [];

  constructor(private cfSvc: CustomFieldService, @Inject(LOCALE_ID) private locale: string) {}

  ngOnChanges(changes: SimpleChanges) {
    if ((changes['entityId'] || changes['domainType'] || changes['entityType']) && this.entityId) {
      this.loadFields();
    }
    if (changes['requiredFields'] && this.baseFields.length) {
      this.applyFilter();
    }
  }

  loadFields() {
    this.loading = true;
    this.loadError = '';
    if (this.domainType) {
      // Use merged loader: all domain definitions + any saved values
      const et = this.entityType || undefined;
      this.cfSvc.getFieldsWithValues(this.domainType, this.entityId, et).subscribe({
        next: f => {
          this.baseFields = f;
          this.applyFilter();
          this.loading = false;
          console.log(`[CustomFields] Loaded ${f.length} fields for domain=${this.domainType}, entityType=${this.entityType || 'all'}, entity=${this.entityId}`);
        },
        error: (err) => { this.fields = []; this.loading = false; this.loadError = 'Failed to load fields'; console.error(`[CustomFields] Error loading fields for domain=${this.domainType}, entity=${this.entityId}`, err); }
      });
    } else {
      // Fallback: only saved values (backwards compatible)
      this.cfSvc.getValuesForEntity(this.entityId).subscribe({
        next: f => {
          this.baseFields = f;
          this.applyFilter();
          this.loading = false;
          console.log(`[CustomFields] Fallback: loaded ${f.length} saved values for entity=${this.entityId} (no domainType provided)`);
        },
        error: (err) => { this.fields = []; this.loading = false; this.loadError = 'Failed to load fields'; console.error(`[CustomFields] Error loading values for entity=${this.entityId}`, err); }
      });
    }
  }

  startEdit(f: CustomFieldValueDto) {
    console.log('[CustomFields] startEdit called for field:', f.fieldName, 'id:', f.customFieldId, 'options:', f.options);
    this.editing = f.customFieldId;
    this.editError = '';
    if (this.isMultiSelectType(f.fieldType)) {
      this.multiSelectValues = f.value ? f.value.split(',').map(v => v.trim()).filter(v => v) : [];
      this.editValue = this.multiSelectValues.join(', ');
    } else if (this.isBooleanType(f.fieldType)) {
      this.editValue = (f.value || '').toLowerCase() === 'true' ? 'true' : 'false';
    } else {
      this.editValue = f.value || '';
    }
    console.log('[CustomFields] Edit mode activated, editValue:', this.editValue);
  }

  cancelEdit() {
    this.editing = null;
    this.editValue = '';
    this.editError = '';
    this.multiSelectValues = [];
  }

  // --- Field type checks ---

  private normalizeType(fieldType: string): string {
    return (fieldType || '').toLowerCase();
  }

  isBooleanType(fieldType: string): boolean {
    return this.normalizeType(fieldType) === 'boolean';
  }

  isMultiSelectType(fieldType: string): boolean {
    return this.normalizeType(fieldType) === 'multiselect';
  }

  isDropdownType(fieldType: string): boolean {
    return this.normalizeType(fieldType) === 'dropdown';
  }

  isCurrencyType(fieldType: string): boolean {
    return this.normalizeType(fieldType) === 'currency';
  }

  isPlainNumberType(fieldType: string): boolean {
    const t = this.normalizeType(fieldType);
    return t === 'number' || t === 'decimal' || t === 'integer';
  }

  isNumberType(fieldType: string): boolean {
    return this.isPlainNumberType(fieldType) || this.isCurrencyType(fieldType);
  }

  isDateType(fieldType: string): boolean {
    const t = this.normalizeType(fieldType);
    return t === 'date' || t === 'datetime';
  }

  isRichTextType(fieldType: string): boolean {
    return this.normalizeType(fieldType) === 'richtext';
  }

  isCalculatedType(fieldType: string): boolean {
    return this.normalizeType(fieldType) === 'calculated';
  }

  isPlainTextType(fieldType: string): boolean {
    const t = this.normalizeType(fieldType);
    // Catch-all: anything not handled by a specific editor
    return t !== 'boolean' && t !== 'multiselect' && t !== 'dropdown' &&
           t !== 'currency' && t !== 'number' && t !== 'decimal' && t !== 'integer' &&
           t !== 'date' && t !== 'datetime' && t !== 'richtext' && t !== 'calculated';
  }

  // --- Options parsing: handles JSON arrays and comma-separated ---

  getOptions(f: CustomFieldValueDto): string[] {
    const opts = this.getOptionsRaw(f.options);
    if (this.isDropdownType(f.fieldType) || this.isMultiSelectType(f.fieldType)) {
      console.log('[CustomFields] getOptions for', f.fieldName, ':', opts, 'raw:', f.options);
    }
    return opts;
  }

  private getOptionsRaw(options: string | undefined | null): string[] {
    if (!options) return [];
    const trimmed = options.trim();
    if (trimmed.startsWith('[')) {
      try {
        const parsed = JSON.parse(trimmed);
        if (Array.isArray(parsed)) {
          return parsed.map((o: any) => String(o).trim()).filter(o => o.length > 0);
        }
      } catch { /* fall through to comma split */ }
    }
    return trimmed.split(',').map(o => o.trim()).filter(o => o.length > 0);
  }

  // --- MultiSelect helpers ---

  isMultiSelected(opt: string): boolean {
    return this.multiSelectValues.includes(opt);
  }

  toggleMultiSelect(opt: string, checked: boolean) {
    if (checked && !this.multiSelectValues.includes(opt)) {
      this.multiSelectValues.push(opt);
    } else if (!checked) {
      this.multiSelectValues = this.multiSelectValues.filter(v => v !== opt);
    }
    this.editValue = this.multiSelectValues.join(', ');
  }

  private applyFilter() {
    if (!this.baseFields?.length) {
      this.fields = [];
      console.log('[CustomFields] applyFilter: no baseFields available');
      return;
    }

    if (!this.requiredFields?.length) {
      this.fields = [...this.baseFields];
      console.log(`[CustomFields] applyFilter: no requiredFields filter, showing all ${this.baseFields.length} fields`);
      return;
    }

    const requiredSet = new Set(this.requiredFields.map(r => r.trim().toLowerCase()).filter(r => r));
    this.fields = this.baseFields.filter(f => requiredSet.has(f.fieldName.toLowerCase()));
    console.log(`[CustomFields] applyFilter: filtered to ${this.fields.length}/${this.baseFields.length} fields by requiredFields=[${this.requiredFields.join(', ')}]`);
  }

  // --- Display formatting ---

  formatDisplay(f: CustomFieldValueDto): string {
    if (!f.value) return '—';
    if (this.isBooleanType(f.fieldType)) {
      return f.value.toLowerCase() === 'true' ? 'Yes' : 'No';
    }
    if (this.isCurrencyType(f.fieldType)) {
      const n = parseFloat(f.value);
      return isNaN(n) ? f.value : '$' + n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }
    if (this.isDateType(f.fieldType)) {
      const d = new Date(f.value);
      return isNaN(d.getTime()) ? f.value : formatDate(d, 'mediumDate', this.locale);
    }
    if (this.isPlainNumberType(f.fieldType)) {
      const n = parseFloat(f.value);
      return isNaN(n) ? f.value : n.toLocaleString();
    }
    return f.value;
  }

  // --- Validation ---

  validate(f: CustomFieldValueDto): string {
    const val = this.editValue.trim();
    if (f.isRequired && !val) {
      return `${f.fieldName} is required`;
    }
    if (val && this.isNumberType(f.fieldType)) {
      if (isNaN(parseFloat(val))) {
        return `${f.fieldName} must be a valid number`;
      }
    }
    if (val && this.isDateType(f.fieldType)) {
      const d = new Date(val);
      if (isNaN(d.getTime())) {
        return `${f.fieldName} must be a valid date`;
      }
    }
    if (val && this.isDropdownType(f.fieldType)) {
      const opts = this.getOptions(f);
      if (opts.length > 0 && !opts.includes(val)) {
        return `${f.fieldName} must be one of: ${opts.join(', ')}`;
      }
    }
    return '';
  }

  saveEdit(f: CustomFieldValueDto) {
    this.editError = this.validate(f);
    if (this.editError) return;

    const value = this.editValue.trim();
    this.cfSvc.saveValue(this.entityId, f.customFieldId, value).subscribe({
      next: () => {
        f.value = value;
        this.editing = null;
        this.editValue = '';
        this.editError = '';
        this.multiSelectValues = [];
      },
      error: () => {
        this.editError = 'Failed to save. Please try again.';
      }
    });
  }
}
