import { onMounted, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { plansApi } from '@/api/modules';
const rows = ref([]);
const loading = ref(false);
const includeInactive = ref(false);
const dialogOpen = ref(false);
const dialogMode = ref('create');
const editingId = ref(null);
const saving = ref(false);
const formRef = ref();
const form = reactive({
    code: '',
    name: '',
    maxStores: 1,
    maxStaff: 10,
    annualPrice: 0,
    featureJson: '',
    isActive: true
});
const rules = {
    code: [{ required: true, message: '请输入编码', trigger: 'blur' }],
    name: [{ required: true, message: '请输入名称', trigger: 'blur' }]
};
async function reload() {
    loading.value = true;
    try {
        rows.value = await plansApi.list(includeInactive.value);
    }
    finally {
        loading.value = false;
    }
}
function openCreate() {
    dialogMode.value = 'create';
    editingId.value = null;
    Object.assign(form, { code: '', name: '', maxStores: 1, maxStaff: 10, annualPrice: 0, featureJson: '', isActive: true });
    dialogOpen.value = true;
}
function openEdit(row) {
    dialogMode.value = 'edit';
    editingId.value = row.id;
    Object.assign(form, {
        code: row.code,
        name: row.name,
        maxStores: row.maxStores,
        maxStaff: row.maxStaff,
        annualPrice: row.annualPrice,
        featureJson: row.featureJson ?? '',
        isActive: row.isActive
    });
    dialogOpen.value = true;
}
async function save() {
    if (!formRef.value)
        return;
    const ok = await formRef.value.validate().catch(() => false);
    if (!ok)
        return;
    saving.value = true;
    try {
        if (dialogMode.value === 'create') {
            await plansApi.create({ ...form, featureJson: form.featureJson || null });
        }
        else if (editingId.value != null) {
            const { code: _code, ...rest } = form;
            await plansApi.update(editingId.value, { ...rest, featureJson: form.featureJson || null });
        }
        ElMessage.success('已保存');
        dialogOpen.value = false;
        reload();
    }
    finally {
        saving.value = false;
    }
}
async function toggle(row) {
    const { code: _code, ...rest } = row;
    await plansApi.update(row.id, { ...rest, isActive: !row.isActive, featureJson: row.featureJson ?? null });
    ElMessage.success(row.isActive ? '已停用' : '已启用');
    reload();
}
onMounted(reload);
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
// CSS variable injection 
// CSS variable injection end 
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "page" },
});
const __VLS_0 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({
    shadow: "never",
}));
const __VLS_2 = __VLS_1({
    shadow: "never",
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
__VLS_3.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "toolbar" },
});
const __VLS_4 = {}.ElCheckbox;
/** @type {[typeof __VLS_components.ElCheckbox, typeof __VLS_components.elCheckbox, typeof __VLS_components.ElCheckbox, typeof __VLS_components.elCheckbox, ]} */ ;
// @ts-ignore
const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.includeInactive),
}));
const __VLS_6 = __VLS_5({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.includeInactive),
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
let __VLS_8;
let __VLS_9;
let __VLS_10;
const __VLS_11 = {
    onChange: (__VLS_ctx.reload)
};
__VLS_7.slots.default;
var __VLS_7;
const __VLS_12 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
    ...{ 'onClick': {} },
    type: "primary",
}));
const __VLS_14 = __VLS_13({
    ...{ 'onClick': {} },
    type: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
let __VLS_16;
let __VLS_17;
let __VLS_18;
const __VLS_19 = {
    onClick: (__VLS_ctx.openCreate)
};
__VLS_15.slots.default;
var __VLS_15;
const __VLS_20 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_22 = __VLS_21({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_21));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_23.slots.default;
const __VLS_24 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
    prop: "code",
    label: "编码",
    width: "120",
}));
const __VLS_26 = __VLS_25({
    prop: "code",
    label: "编码",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_25));
const __VLS_28 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
    prop: "name",
    label: "名称",
    minWidth: "160",
}));
const __VLS_30 = __VLS_29({
    prop: "name",
    label: "名称",
    minWidth: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
const __VLS_32 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
    prop: "maxStores",
    label: "门店上限",
    width: "100",
}));
const __VLS_34 = __VLS_33({
    prop: "maxStores",
    label: "门店上限",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_33));
const __VLS_36 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
    prop: "maxStaff",
    label: "员工上限",
    width: "100",
}));
const __VLS_38 = __VLS_37({
    prop: "maxStaff",
    label: "员工上限",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
const __VLS_40 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
    label: "年费",
    width: "120",
}));
const __VLS_42 = __VLS_41({
    label: "年费",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_41));
__VLS_43.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_43.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.annualPrice.toFixed(2));
}
var __VLS_43;
const __VLS_44 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    label: "状态",
    width: "100",
}));
const __VLS_46 = __VLS_45({
    label: "状态",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
__VLS_47.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_47.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_48 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
        type: (row.isActive ? 'success' : 'info'),
    }));
    const __VLS_50 = __VLS_49({
        type: (row.isActive ? 'success' : 'info'),
    }, ...__VLS_functionalComponentArgsRest(__VLS_49));
    __VLS_51.slots.default;
    (row.isActive ? '启用' : '停用');
    var __VLS_51;
}
var __VLS_47;
const __VLS_52 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
    label: "操作",
    width: "160",
}));
const __VLS_54 = __VLS_53({
    label: "操作",
    width: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_53));
__VLS_55.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_55.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_56 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
    }));
    const __VLS_58 = __VLS_57({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
    }, ...__VLS_functionalComponentArgsRest(__VLS_57));
    let __VLS_60;
    let __VLS_61;
    let __VLS_62;
    const __VLS_63 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openEdit(row);
        }
    };
    __VLS_59.slots.default;
    var __VLS_59;
    const __VLS_64 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
        ...{ 'onClick': {} },
        link: true,
        type: (row.isActive ? 'warning' : 'success'),
    }));
    const __VLS_66 = __VLS_65({
        ...{ 'onClick': {} },
        link: true,
        type: (row.isActive ? 'warning' : 'success'),
    }, ...__VLS_functionalComponentArgsRest(__VLS_65));
    let __VLS_68;
    let __VLS_69;
    let __VLS_70;
    const __VLS_71 = {
        onClick: (...[$event]) => {
            __VLS_ctx.toggle(row);
        }
    };
    __VLS_67.slots.default;
    (row.isActive ? '停用' : '启用');
    var __VLS_67;
}
var __VLS_55;
var __VLS_23;
var __VLS_3;
const __VLS_72 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
    modelValue: (__VLS_ctx.dialogOpen),
    title: (__VLS_ctx.dialogMode === 'create' ? '新建套餐' : '编辑套餐'),
    width: "480px",
}));
const __VLS_74 = __VLS_73({
    modelValue: (__VLS_ctx.dialogOpen),
    title: (__VLS_ctx.dialogMode === 'create' ? '新建套餐' : '编辑套餐'),
    width: "480px",
}, ...__VLS_functionalComponentArgsRest(__VLS_73));
__VLS_75.slots.default;
const __VLS_76 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
    model: (__VLS_ctx.form),
    rules: (__VLS_ctx.rules),
    ref: "formRef",
    labelWidth: "100px",
}));
const __VLS_78 = __VLS_77({
    model: (__VLS_ctx.form),
    rules: (__VLS_ctx.rules),
    ref: "formRef",
    labelWidth: "100px",
}, ...__VLS_functionalComponentArgsRest(__VLS_77));
/** @type {typeof __VLS_ctx.formRef} */ ;
var __VLS_80 = {};
__VLS_79.slots.default;
const __VLS_82 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_83 = __VLS_asFunctionalComponent(__VLS_82, new __VLS_82({
    label: "编码",
    prop: "code",
}));
const __VLS_84 = __VLS_83({
    label: "编码",
    prop: "code",
}, ...__VLS_functionalComponentArgsRest(__VLS_83));
__VLS_85.slots.default;
const __VLS_86 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_87 = __VLS_asFunctionalComponent(__VLS_86, new __VLS_86({
    modelValue: (__VLS_ctx.form.code),
    disabled: (__VLS_ctx.dialogMode === 'edit'),
}));
const __VLS_88 = __VLS_87({
    modelValue: (__VLS_ctx.form.code),
    disabled: (__VLS_ctx.dialogMode === 'edit'),
}, ...__VLS_functionalComponentArgsRest(__VLS_87));
var __VLS_85;
const __VLS_90 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_91 = __VLS_asFunctionalComponent(__VLS_90, new __VLS_90({
    label: "名称",
    prop: "name",
}));
const __VLS_92 = __VLS_91({
    label: "名称",
    prop: "name",
}, ...__VLS_functionalComponentArgsRest(__VLS_91));
__VLS_93.slots.default;
const __VLS_94 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_95 = __VLS_asFunctionalComponent(__VLS_94, new __VLS_94({
    modelValue: (__VLS_ctx.form.name),
}));
const __VLS_96 = __VLS_95({
    modelValue: (__VLS_ctx.form.name),
}, ...__VLS_functionalComponentArgsRest(__VLS_95));
var __VLS_93;
const __VLS_98 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_99 = __VLS_asFunctionalComponent(__VLS_98, new __VLS_98({
    label: "门店上限",
    prop: "maxStores",
}));
const __VLS_100 = __VLS_99({
    label: "门店上限",
    prop: "maxStores",
}, ...__VLS_functionalComponentArgsRest(__VLS_99));
__VLS_101.slots.default;
const __VLS_102 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_103 = __VLS_asFunctionalComponent(__VLS_102, new __VLS_102({
    modelValue: (__VLS_ctx.form.maxStores),
    min: (1),
}));
const __VLS_104 = __VLS_103({
    modelValue: (__VLS_ctx.form.maxStores),
    min: (1),
}, ...__VLS_functionalComponentArgsRest(__VLS_103));
var __VLS_101;
const __VLS_106 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_107 = __VLS_asFunctionalComponent(__VLS_106, new __VLS_106({
    label: "员工上限",
    prop: "maxStaff",
}));
const __VLS_108 = __VLS_107({
    label: "员工上限",
    prop: "maxStaff",
}, ...__VLS_functionalComponentArgsRest(__VLS_107));
__VLS_109.slots.default;
const __VLS_110 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_111 = __VLS_asFunctionalComponent(__VLS_110, new __VLS_110({
    modelValue: (__VLS_ctx.form.maxStaff),
    min: (1),
}));
const __VLS_112 = __VLS_111({
    modelValue: (__VLS_ctx.form.maxStaff),
    min: (1),
}, ...__VLS_functionalComponentArgsRest(__VLS_111));
var __VLS_109;
const __VLS_114 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_115 = __VLS_asFunctionalComponent(__VLS_114, new __VLS_114({
    label: "年费(元)",
    prop: "annualPrice",
}));
const __VLS_116 = __VLS_115({
    label: "年费(元)",
    prop: "annualPrice",
}, ...__VLS_functionalComponentArgsRest(__VLS_115));
__VLS_117.slots.default;
const __VLS_118 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_119 = __VLS_asFunctionalComponent(__VLS_118, new __VLS_118({
    modelValue: (__VLS_ctx.form.annualPrice),
    min: (0),
    precision: (2),
    step: (100),
}));
const __VLS_120 = __VLS_119({
    modelValue: (__VLS_ctx.form.annualPrice),
    min: (0),
    precision: (2),
    step: (100),
}, ...__VLS_functionalComponentArgsRest(__VLS_119));
var __VLS_117;
const __VLS_122 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_123 = __VLS_asFunctionalComponent(__VLS_122, new __VLS_122({
    label: "启用",
}));
const __VLS_124 = __VLS_123({
    label: "启用",
}, ...__VLS_functionalComponentArgsRest(__VLS_123));
__VLS_125.slots.default;
const __VLS_126 = {}.ElSwitch;
/** @type {[typeof __VLS_components.ElSwitch, typeof __VLS_components.elSwitch, ]} */ ;
// @ts-ignore
const __VLS_127 = __VLS_asFunctionalComponent(__VLS_126, new __VLS_126({
    modelValue: (__VLS_ctx.form.isActive),
}));
const __VLS_128 = __VLS_127({
    modelValue: (__VLS_ctx.form.isActive),
}, ...__VLS_functionalComponentArgsRest(__VLS_127));
var __VLS_125;
var __VLS_79;
{
    const { footer: __VLS_thisSlot } = __VLS_75.slots;
    const __VLS_130 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_131 = __VLS_asFunctionalComponent(__VLS_130, new __VLS_130({
        ...{ 'onClick': {} },
    }));
    const __VLS_132 = __VLS_131({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_131));
    let __VLS_134;
    let __VLS_135;
    let __VLS_136;
    const __VLS_137 = {
        onClick: (...[$event]) => {
            __VLS_ctx.dialogOpen = false;
        }
    };
    __VLS_133.slots.default;
    var __VLS_133;
    const __VLS_138 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_139 = __VLS_asFunctionalComponent(__VLS_138, new __VLS_138({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_140 = __VLS_139({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_139));
    let __VLS_142;
    let __VLS_143;
    let __VLS_144;
    const __VLS_145 = {
        onClick: (__VLS_ctx.save)
    };
    __VLS_141.slots.default;
    var __VLS_141;
}
var __VLS_75;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
// @ts-ignore
var __VLS_81 = __VLS_80;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            rows: rows,
            loading: loading,
            includeInactive: includeInactive,
            dialogOpen: dialogOpen,
            dialogMode: dialogMode,
            saving: saving,
            formRef: formRef,
            form: form,
            rules: rules,
            reload: reload,
            openCreate: openCreate,
            openEdit: openEdit,
            save: save,
            toggle: toggle,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
