import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { staffApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
const appStore = useAppStore();
const rows = ref([]);
const total = ref(0);
const loading = ref(false);
const saving = ref(false);
const query = reactive({ page: 1, pageSize: 20, keyword: '', role: '', storeId: null });
const formOpen = ref(false);
const formMode = ref('create');
const editingId = ref(null);
const formRef = ref();
const form = reactive({
    username: '',
    password: '',
    realName: '',
    phone: '',
    employeeNo: null,
    role: 'Technician',
    storeId: null,
    isBlind: false,
    isActive: true
});
const rules = {
    username: [{ required: true, message: '请输入账号', trigger: 'blur' }],
    password: [{ required: true, message: '请输入密码', trigger: 'blur' }, { min: 6, message: '至少 6 位', trigger: 'blur' }],
    role: [{ required: true, message: '请选择角色', trigger: 'change' }],
    storeId: [{ required: true, message: '请选择门店', trigger: 'change' }]
};
const pwdOpen = ref(false);
const pwdTarget = ref(null);
const newPassword = ref('');
function roleLabel(r) {
    return { ShopOwner: '店主', StoreManager: '店长', Cashier: '收银员', Technician: '技师', PlatformAdmin: '平台' }[r] ?? r;
}
const storeMap = computed(() => Object.fromEntries(appStore.stores.map((s) => [s.id, s.name])));
function storeName(id) {
    if (!id)
        return '—';
    return storeMap.value[id] ?? `#${id}`;
}
async function reload() {
    loading.value = true;
    try {
        const data = await staffApi.list({
            page: query.page,
            pageSize: query.pageSize,
            keyword: query.keyword || undefined,
            role: query.role || undefined,
            storeId: query.storeId ?? undefined
        });
        rows.value = data.items;
        total.value = data.total;
    }
    finally {
        loading.value = false;
    }
}
function resetQuery() {
    query.keyword = '';
    query.role = '';
    query.storeId = null;
    query.page = 1;
    reload();
}
function openCreate() {
    formMode.value = 'create';
    editingId.value = null;
    Object.assign(form, {
        username: '', password: '', realName: '', phone: '',
        employeeNo: null, role: 'Technician',
        storeId: appStore.activeStoreId,
        isBlind: false, isActive: true
    });
    formOpen.value = true;
}
function openEdit(row) {
    formMode.value = 'edit';
    editingId.value = row.id;
    Object.assign(form, {
        username: row.username,
        password: '',
        realName: row.realName ?? '',
        phone: row.phone ?? '',
        employeeNo: row.employeeNo ?? null,
        role: row.role,
        storeId: row.storeId ?? null,
        isBlind: row.isBlind,
        isActive: row.isActive
    });
    formOpen.value = true;
}
async function save() {
    if (!formRef.value)
        return;
    const ok = await formRef.value.validate().catch(() => false);
    if (!ok)
        return;
    saving.value = true;
    try {
        if (formMode.value === 'create') {
            await staffApi.create({
                username: form.username,
                password: form.password,
                realName: form.realName || null,
                phone: form.phone || null,
                employeeNo: form.employeeNo,
                role: form.role,
                storeId: form.storeId,
                isBlind: form.isBlind
            });
        }
        else if (editingId.value != null) {
            await staffApi.update(editingId.value, {
                realName: form.realName || null,
                phone: form.phone || null,
                employeeNo: form.employeeNo,
                role: form.role,
                storeId: form.storeId,
                isBlind: form.isBlind,
                isActive: form.isActive
            });
        }
        ElMessage.success('已保存');
        formOpen.value = false;
        reload();
    }
    finally {
        saving.value = false;
    }
}
function openResetPwd(row) {
    pwdTarget.value = row;
    newPassword.value = '';
    pwdOpen.value = true;
}
async function doResetPwd() {
    if (!pwdTarget.value)
        return;
    if (newPassword.value.length < 6) {
        ElMessage.warning('密码至少 6 位');
        return;
    }
    saving.value = true;
    try {
        await staffApi.resetPassword(pwdTarget.value.id, newPassword.value);
        ElMessage.success('密码已重置');
        pwdOpen.value = false;
    }
    finally {
        saving.value = false;
    }
}
onMounted(async () => {
    await appStore.loadStores();
    reload();
});
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
const __VLS_4 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
    ...{ 'onKeyup': {} },
    modelValue: (__VLS_ctx.query.keyword),
    placeholder: "账号 / 姓名 / 手机号",
    clearable: true,
    ...{ style: {} },
}));
const __VLS_6 = __VLS_5({
    ...{ 'onKeyup': {} },
    modelValue: (__VLS_ctx.query.keyword),
    placeholder: "账号 / 姓名 / 手机号",
    clearable: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
let __VLS_8;
let __VLS_9;
let __VLS_10;
const __VLS_11 = {
    onKeyup: (__VLS_ctx.reload)
};
var __VLS_7;
const __VLS_12 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
    modelValue: (__VLS_ctx.query.role),
    placeholder: "全部角色",
    clearable: true,
    ...{ style: {} },
}));
const __VLS_14 = __VLS_13({
    modelValue: (__VLS_ctx.query.role),
    placeholder: "全部角色",
    clearable: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
__VLS_15.slots.default;
const __VLS_16 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({
    label: "店主",
    value: "ShopOwner",
}));
const __VLS_18 = __VLS_17({
    label: "店主",
    value: "ShopOwner",
}, ...__VLS_functionalComponentArgsRest(__VLS_17));
const __VLS_20 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
    label: "店长",
    value: "StoreManager",
}));
const __VLS_22 = __VLS_21({
    label: "店长",
    value: "StoreManager",
}, ...__VLS_functionalComponentArgsRest(__VLS_21));
const __VLS_24 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
    label: "收银员",
    value: "Cashier",
}));
const __VLS_26 = __VLS_25({
    label: "收银员",
    value: "Cashier",
}, ...__VLS_functionalComponentArgsRest(__VLS_25));
const __VLS_28 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
    label: "技师",
    value: "Technician",
}));
const __VLS_30 = __VLS_29({
    label: "技师",
    value: "Technician",
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
var __VLS_15;
const __VLS_32 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
    modelValue: (__VLS_ctx.query.storeId),
    placeholder: "全部门店",
    clearable: true,
    ...{ style: {} },
}));
const __VLS_34 = __VLS_33({
    modelValue: (__VLS_ctx.query.storeId),
    placeholder: "全部门店",
    clearable: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_33));
__VLS_35.slots.default;
for (const [s] of __VLS_getVForSourceType((__VLS_ctx.appStore.stores))) {
    const __VLS_36 = {}.ElOption;
    /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
    // @ts-ignore
    const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
        key: (s.id),
        label: (s.name),
        value: (s.id),
    }));
    const __VLS_38 = __VLS_37({
        key: (s.id),
        label: (s.name),
        value: (s.id),
    }, ...__VLS_functionalComponentArgsRest(__VLS_37));
}
var __VLS_35;
const __VLS_40 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
    ...{ 'onClick': {} },
    type: "primary",
}));
const __VLS_42 = __VLS_41({
    ...{ 'onClick': {} },
    type: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_41));
let __VLS_44;
let __VLS_45;
let __VLS_46;
const __VLS_47 = {
    onClick: (__VLS_ctx.reload)
};
__VLS_43.slots.default;
var __VLS_43;
const __VLS_48 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
    ...{ 'onClick': {} },
}));
const __VLS_50 = __VLS_49({
    ...{ 'onClick': {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_49));
let __VLS_52;
let __VLS_53;
let __VLS_54;
const __VLS_55 = {
    onClick: (__VLS_ctx.resetQuery)
};
__VLS_51.slots.default;
var __VLS_51;
const __VLS_56 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
    ...{ 'onClick': {} },
    type: "success",
}));
const __VLS_58 = __VLS_57({
    ...{ 'onClick': {} },
    type: "success",
}, ...__VLS_functionalComponentArgsRest(__VLS_57));
let __VLS_60;
let __VLS_61;
let __VLS_62;
const __VLS_63 = {
    onClick: (__VLS_ctx.openCreate)
};
__VLS_59.slots.default;
var __VLS_59;
const __VLS_64 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_66 = __VLS_65({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_65));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_67.slots.default;
const __VLS_68 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
    prop: "employeeNo",
    label: "工号",
    width: "80",
}));
const __VLS_70 = __VLS_69({
    prop: "employeeNo",
    label: "工号",
    width: "80",
}, ...__VLS_functionalComponentArgsRest(__VLS_69));
const __VLS_72 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
    prop: "username",
    label: "账号",
    width: "120",
}));
const __VLS_74 = __VLS_73({
    prop: "username",
    label: "账号",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_73));
const __VLS_76 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
    prop: "realName",
    label: "姓名",
    width: "100",
}));
const __VLS_78 = __VLS_77({
    prop: "realName",
    label: "姓名",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_77));
const __VLS_80 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
    prop: "phone",
    label: "手机号",
    width: "130",
}));
const __VLS_82 = __VLS_81({
    prop: "phone",
    label: "手机号",
    width: "130",
}, ...__VLS_functionalComponentArgsRest(__VLS_81));
const __VLS_84 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_85 = __VLS_asFunctionalComponent(__VLS_84, new __VLS_84({
    label: "角色",
    width: "100",
}));
const __VLS_86 = __VLS_85({
    label: "角色",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_85));
__VLS_87.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_87.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (__VLS_ctx.roleLabel(row.role));
}
var __VLS_87;
const __VLS_88 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
    label: "所属门店",
    width: "140",
}));
const __VLS_90 = __VLS_89({
    label: "所属门店",
    width: "140",
}, ...__VLS_functionalComponentArgsRest(__VLS_89));
__VLS_91.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_91.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (__VLS_ctx.storeName(row.storeId));
}
var __VLS_91;
const __VLS_92 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_93 = __VLS_asFunctionalComponent(__VLS_92, new __VLS_92({
    label: "盲人",
    width: "80",
}));
const __VLS_94 = __VLS_93({
    label: "盲人",
    width: "80",
}, ...__VLS_functionalComponentArgsRest(__VLS_93));
__VLS_95.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_95.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    if (row.isBlind) {
        const __VLS_96 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({
            size: "small",
        }));
        const __VLS_98 = __VLS_97({
            size: "small",
        }, ...__VLS_functionalComponentArgsRest(__VLS_97));
        __VLS_99.slots.default;
        var __VLS_99;
    }
    else {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    }
}
var __VLS_95;
const __VLS_100 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_101 = __VLS_asFunctionalComponent(__VLS_100, new __VLS_100({
    label: "状态",
    width: "80",
}));
const __VLS_102 = __VLS_101({
    label: "状态",
    width: "80",
}, ...__VLS_functionalComponentArgsRest(__VLS_101));
__VLS_103.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_103.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_104 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_105 = __VLS_asFunctionalComponent(__VLS_104, new __VLS_104({
        type: (row.isActive ? 'success' : 'info'),
    }));
    const __VLS_106 = __VLS_105({
        type: (row.isActive ? 'success' : 'info'),
    }, ...__VLS_functionalComponentArgsRest(__VLS_105));
    __VLS_107.slots.default;
    (row.isActive ? '在职' : '停用');
    var __VLS_107;
}
var __VLS_103;
const __VLS_108 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_109 = __VLS_asFunctionalComponent(__VLS_108, new __VLS_108({
    label: "操作",
    width: "220",
    fixed: "right",
}));
const __VLS_110 = __VLS_109({
    label: "操作",
    width: "220",
    fixed: "right",
}, ...__VLS_functionalComponentArgsRest(__VLS_109));
__VLS_111.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_111.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_112 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
    }));
    const __VLS_114 = __VLS_113({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
    }, ...__VLS_functionalComponentArgsRest(__VLS_113));
    let __VLS_116;
    let __VLS_117;
    let __VLS_118;
    const __VLS_119 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openEdit(row);
        }
    };
    __VLS_115.slots.default;
    var __VLS_115;
    const __VLS_120 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_121 = __VLS_asFunctionalComponent(__VLS_120, new __VLS_120({
        ...{ 'onClick': {} },
        link: true,
        type: "warning",
    }));
    const __VLS_122 = __VLS_121({
        ...{ 'onClick': {} },
        link: true,
        type: "warning",
    }, ...__VLS_functionalComponentArgsRest(__VLS_121));
    let __VLS_124;
    let __VLS_125;
    let __VLS_126;
    const __VLS_127 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openResetPwd(row);
        }
    };
    __VLS_123.slots.default;
    var __VLS_123;
}
var __VLS_111;
var __VLS_67;
const __VLS_128 = {}.ElPagination;
/** @type {[typeof __VLS_components.ElPagination, typeof __VLS_components.elPagination, ]} */ ;
// @ts-ignore
const __VLS_129 = __VLS_asFunctionalComponent(__VLS_128, new __VLS_128({
    ...{ 'onCurrentChange': {} },
    ...{ 'onSizeChange': {} },
    ...{ style: {} },
    currentPage: (__VLS_ctx.query.page),
    pageSize: (__VLS_ctx.query.pageSize),
    total: (__VLS_ctx.total),
    pageSizes: ([10, 20, 50]),
    layout: "total, sizes, prev, pager, next, jumper",
}));
const __VLS_130 = __VLS_129({
    ...{ 'onCurrentChange': {} },
    ...{ 'onSizeChange': {} },
    ...{ style: {} },
    currentPage: (__VLS_ctx.query.page),
    pageSize: (__VLS_ctx.query.pageSize),
    total: (__VLS_ctx.total),
    pageSizes: ([10, 20, 50]),
    layout: "total, sizes, prev, pager, next, jumper",
}, ...__VLS_functionalComponentArgsRest(__VLS_129));
let __VLS_132;
let __VLS_133;
let __VLS_134;
const __VLS_135 = {
    onCurrentChange: ((p) => { __VLS_ctx.query.page = p; __VLS_ctx.reload(); })
};
const __VLS_136 = {
    onSizeChange: ((s) => { __VLS_ctx.query.pageSize = s; __VLS_ctx.query.page = 1; __VLS_ctx.reload(); })
};
var __VLS_131;
var __VLS_3;
const __VLS_137 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_138 = __VLS_asFunctionalComponent(__VLS_137, new __VLS_137({
    modelValue: (__VLS_ctx.formOpen),
    title: (__VLS_ctx.formMode === 'create' ? '添加员工' : '编辑员工'),
    width: "480px",
}));
const __VLS_139 = __VLS_138({
    modelValue: (__VLS_ctx.formOpen),
    title: (__VLS_ctx.formMode === 'create' ? '添加员工' : '编辑员工'),
    width: "480px",
}, ...__VLS_functionalComponentArgsRest(__VLS_138));
__VLS_140.slots.default;
const __VLS_141 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_142 = __VLS_asFunctionalComponent(__VLS_141, new __VLS_141({
    model: (__VLS_ctx.form),
    rules: (__VLS_ctx.rules),
    ref: "formRef",
    labelWidth: "100px",
}));
const __VLS_143 = __VLS_142({
    model: (__VLS_ctx.form),
    rules: (__VLS_ctx.rules),
    ref: "formRef",
    labelWidth: "100px",
}, ...__VLS_functionalComponentArgsRest(__VLS_142));
/** @type {typeof __VLS_ctx.formRef} */ ;
var __VLS_145 = {};
__VLS_144.slots.default;
const __VLS_147 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_148 = __VLS_asFunctionalComponent(__VLS_147, new __VLS_147({
    label: "账号",
    prop: "username",
}));
const __VLS_149 = __VLS_148({
    label: "账号",
    prop: "username",
}, ...__VLS_functionalComponentArgsRest(__VLS_148));
__VLS_150.slots.default;
const __VLS_151 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_152 = __VLS_asFunctionalComponent(__VLS_151, new __VLS_151({
    modelValue: (__VLS_ctx.form.username),
    disabled: (__VLS_ctx.formMode === 'edit'),
}));
const __VLS_153 = __VLS_152({
    modelValue: (__VLS_ctx.form.username),
    disabled: (__VLS_ctx.formMode === 'edit'),
}, ...__VLS_functionalComponentArgsRest(__VLS_152));
var __VLS_150;
if (__VLS_ctx.formMode === 'create') {
    const __VLS_155 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_156 = __VLS_asFunctionalComponent(__VLS_155, new __VLS_155({
        label: "初始密码",
        prop: "password",
    }));
    const __VLS_157 = __VLS_156({
        label: "初始密码",
        prop: "password",
    }, ...__VLS_functionalComponentArgsRest(__VLS_156));
    __VLS_158.slots.default;
    const __VLS_159 = {}.ElInput;
    /** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
    // @ts-ignore
    const __VLS_160 = __VLS_asFunctionalComponent(__VLS_159, new __VLS_159({
        modelValue: (__VLS_ctx.form.password),
        type: "password",
        showPassword: true,
    }));
    const __VLS_161 = __VLS_160({
        modelValue: (__VLS_ctx.form.password),
        type: "password",
        showPassword: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_160));
    var __VLS_158;
}
const __VLS_163 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_164 = __VLS_asFunctionalComponent(__VLS_163, new __VLS_163({
    label: "姓名",
}));
const __VLS_165 = __VLS_164({
    label: "姓名",
}, ...__VLS_functionalComponentArgsRest(__VLS_164));
__VLS_166.slots.default;
const __VLS_167 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_168 = __VLS_asFunctionalComponent(__VLS_167, new __VLS_167({
    modelValue: (__VLS_ctx.form.realName),
}));
const __VLS_169 = __VLS_168({
    modelValue: (__VLS_ctx.form.realName),
}, ...__VLS_functionalComponentArgsRest(__VLS_168));
var __VLS_166;
const __VLS_171 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_172 = __VLS_asFunctionalComponent(__VLS_171, new __VLS_171({
    label: "手机号",
}));
const __VLS_173 = __VLS_172({
    label: "手机号",
}, ...__VLS_functionalComponentArgsRest(__VLS_172));
__VLS_174.slots.default;
const __VLS_175 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_176 = __VLS_asFunctionalComponent(__VLS_175, new __VLS_175({
    modelValue: (__VLS_ctx.form.phone),
}));
const __VLS_177 = __VLS_176({
    modelValue: (__VLS_ctx.form.phone),
}, ...__VLS_functionalComponentArgsRest(__VLS_176));
var __VLS_174;
const __VLS_179 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_180 = __VLS_asFunctionalComponent(__VLS_179, new __VLS_179({
    label: "工号",
}));
const __VLS_181 = __VLS_180({
    label: "工号",
}, ...__VLS_functionalComponentArgsRest(__VLS_180));
__VLS_182.slots.default;
const __VLS_183 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_184 = __VLS_asFunctionalComponent(__VLS_183, new __VLS_183({
    modelValue: (__VLS_ctx.form.employeeNo),
    min: (0),
    precision: (0),
}));
const __VLS_185 = __VLS_184({
    modelValue: (__VLS_ctx.form.employeeNo),
    min: (0),
    precision: (0),
}, ...__VLS_functionalComponentArgsRest(__VLS_184));
var __VLS_182;
const __VLS_187 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_188 = __VLS_asFunctionalComponent(__VLS_187, new __VLS_187({
    label: "角色",
    prop: "role",
}));
const __VLS_189 = __VLS_188({
    label: "角色",
    prop: "role",
}, ...__VLS_functionalComponentArgsRest(__VLS_188));
__VLS_190.slots.default;
const __VLS_191 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_192 = __VLS_asFunctionalComponent(__VLS_191, new __VLS_191({
    modelValue: (__VLS_ctx.form.role),
    ...{ style: {} },
}));
const __VLS_193 = __VLS_192({
    modelValue: (__VLS_ctx.form.role),
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_192));
__VLS_194.slots.default;
const __VLS_195 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_196 = __VLS_asFunctionalComponent(__VLS_195, new __VLS_195({
    label: "店主",
    value: "ShopOwner",
}));
const __VLS_197 = __VLS_196({
    label: "店主",
    value: "ShopOwner",
}, ...__VLS_functionalComponentArgsRest(__VLS_196));
const __VLS_199 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_200 = __VLS_asFunctionalComponent(__VLS_199, new __VLS_199({
    label: "店长",
    value: "StoreManager",
}));
const __VLS_201 = __VLS_200({
    label: "店长",
    value: "StoreManager",
}, ...__VLS_functionalComponentArgsRest(__VLS_200));
const __VLS_203 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_204 = __VLS_asFunctionalComponent(__VLS_203, new __VLS_203({
    label: "收银员",
    value: "Cashier",
}));
const __VLS_205 = __VLS_204({
    label: "收银员",
    value: "Cashier",
}, ...__VLS_functionalComponentArgsRest(__VLS_204));
const __VLS_207 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_208 = __VLS_asFunctionalComponent(__VLS_207, new __VLS_207({
    label: "技师",
    value: "Technician",
}));
const __VLS_209 = __VLS_208({
    label: "技师",
    value: "Technician",
}, ...__VLS_functionalComponentArgsRest(__VLS_208));
var __VLS_194;
var __VLS_190;
const __VLS_211 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_212 = __VLS_asFunctionalComponent(__VLS_211, new __VLS_211({
    label: "所属门店",
    prop: "storeId",
}));
const __VLS_213 = __VLS_212({
    label: "所属门店",
    prop: "storeId",
}, ...__VLS_functionalComponentArgsRest(__VLS_212));
__VLS_214.slots.default;
const __VLS_215 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_216 = __VLS_asFunctionalComponent(__VLS_215, new __VLS_215({
    modelValue: (__VLS_ctx.form.storeId),
    ...{ style: {} },
}));
const __VLS_217 = __VLS_216({
    modelValue: (__VLS_ctx.form.storeId),
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_216));
__VLS_218.slots.default;
for (const [s] of __VLS_getVForSourceType((__VLS_ctx.appStore.stores))) {
    const __VLS_219 = {}.ElOption;
    /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
    // @ts-ignore
    const __VLS_220 = __VLS_asFunctionalComponent(__VLS_219, new __VLS_219({
        key: (s.id),
        label: (s.name),
        value: (s.id),
    }));
    const __VLS_221 = __VLS_220({
        key: (s.id),
        label: (s.name),
        value: (s.id),
    }, ...__VLS_functionalComponentArgsRest(__VLS_220));
}
var __VLS_218;
var __VLS_214;
const __VLS_223 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_224 = __VLS_asFunctionalComponent(__VLS_223, new __VLS_223({
    label: "盲人技师",
}));
const __VLS_225 = __VLS_224({
    label: "盲人技师",
}, ...__VLS_functionalComponentArgsRest(__VLS_224));
__VLS_226.slots.default;
const __VLS_227 = {}.ElSwitch;
/** @type {[typeof __VLS_components.ElSwitch, typeof __VLS_components.elSwitch, ]} */ ;
// @ts-ignore
const __VLS_228 = __VLS_asFunctionalComponent(__VLS_227, new __VLS_227({
    modelValue: (__VLS_ctx.form.isBlind),
}));
const __VLS_229 = __VLS_228({
    modelValue: (__VLS_ctx.form.isBlind),
}, ...__VLS_functionalComponentArgsRest(__VLS_228));
var __VLS_226;
if (__VLS_ctx.formMode === 'edit') {
    const __VLS_231 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_232 = __VLS_asFunctionalComponent(__VLS_231, new __VLS_231({
        label: "状态",
    }));
    const __VLS_233 = __VLS_232({
        label: "状态",
    }, ...__VLS_functionalComponentArgsRest(__VLS_232));
    __VLS_234.slots.default;
    const __VLS_235 = {}.ElSwitch;
    /** @type {[typeof __VLS_components.ElSwitch, typeof __VLS_components.elSwitch, ]} */ ;
    // @ts-ignore
    const __VLS_236 = __VLS_asFunctionalComponent(__VLS_235, new __VLS_235({
        modelValue: (__VLS_ctx.form.isActive),
        activeText: "在职",
        inactiveText: "停用",
    }));
    const __VLS_237 = __VLS_236({
        modelValue: (__VLS_ctx.form.isActive),
        activeText: "在职",
        inactiveText: "停用",
    }, ...__VLS_functionalComponentArgsRest(__VLS_236));
    var __VLS_234;
}
var __VLS_144;
{
    const { footer: __VLS_thisSlot } = __VLS_140.slots;
    const __VLS_239 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_240 = __VLS_asFunctionalComponent(__VLS_239, new __VLS_239({
        ...{ 'onClick': {} },
    }));
    const __VLS_241 = __VLS_240({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_240));
    let __VLS_243;
    let __VLS_244;
    let __VLS_245;
    const __VLS_246 = {
        onClick: (...[$event]) => {
            __VLS_ctx.formOpen = false;
        }
    };
    __VLS_242.slots.default;
    var __VLS_242;
    const __VLS_247 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_248 = __VLS_asFunctionalComponent(__VLS_247, new __VLS_247({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_249 = __VLS_248({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_248));
    let __VLS_251;
    let __VLS_252;
    let __VLS_253;
    const __VLS_254 = {
        onClick: (__VLS_ctx.save)
    };
    __VLS_250.slots.default;
    var __VLS_250;
}
var __VLS_140;
const __VLS_255 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_256 = __VLS_asFunctionalComponent(__VLS_255, new __VLS_255({
    modelValue: (__VLS_ctx.pwdOpen),
    title: (`重置密码：${__VLS_ctx.pwdTarget?.username}`),
    width: "380px",
}));
const __VLS_257 = __VLS_256({
    modelValue: (__VLS_ctx.pwdOpen),
    title: (`重置密码：${__VLS_ctx.pwdTarget?.username}`),
    width: "380px",
}, ...__VLS_functionalComponentArgsRest(__VLS_256));
__VLS_258.slots.default;
const __VLS_259 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_260 = __VLS_asFunctionalComponent(__VLS_259, new __VLS_259({}));
const __VLS_261 = __VLS_260({}, ...__VLS_functionalComponentArgsRest(__VLS_260));
__VLS_262.slots.default;
const __VLS_263 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_264 = __VLS_asFunctionalComponent(__VLS_263, new __VLS_263({
    label: "新密码",
}));
const __VLS_265 = __VLS_264({
    label: "新密码",
}, ...__VLS_functionalComponentArgsRest(__VLS_264));
__VLS_266.slots.default;
const __VLS_267 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_268 = __VLS_asFunctionalComponent(__VLS_267, new __VLS_267({
    modelValue: (__VLS_ctx.newPassword),
    type: "password",
    showPassword: true,
    placeholder: "至少 6 位",
}));
const __VLS_269 = __VLS_268({
    modelValue: (__VLS_ctx.newPassword),
    type: "password",
    showPassword: true,
    placeholder: "至少 6 位",
}, ...__VLS_functionalComponentArgsRest(__VLS_268));
var __VLS_266;
var __VLS_262;
{
    const { footer: __VLS_thisSlot } = __VLS_258.slots;
    const __VLS_271 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_272 = __VLS_asFunctionalComponent(__VLS_271, new __VLS_271({
        ...{ 'onClick': {} },
    }));
    const __VLS_273 = __VLS_272({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_272));
    let __VLS_275;
    let __VLS_276;
    let __VLS_277;
    const __VLS_278 = {
        onClick: (...[$event]) => {
            __VLS_ctx.pwdOpen = false;
        }
    };
    __VLS_274.slots.default;
    var __VLS_274;
    const __VLS_279 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_280 = __VLS_asFunctionalComponent(__VLS_279, new __VLS_279({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_281 = __VLS_280({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_280));
    let __VLS_283;
    let __VLS_284;
    let __VLS_285;
    const __VLS_286 = {
        onClick: (__VLS_ctx.doResetPwd)
    };
    __VLS_282.slots.default;
    var __VLS_282;
}
var __VLS_258;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
// @ts-ignore
var __VLS_146 = __VLS_145;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            appStore: appStore,
            rows: rows,
            total: total,
            loading: loading,
            saving: saving,
            query: query,
            formOpen: formOpen,
            formMode: formMode,
            formRef: formRef,
            form: form,
            rules: rules,
            pwdOpen: pwdOpen,
            pwdTarget: pwdTarget,
            newPassword: newPassword,
            roleLabel: roleLabel,
            storeName: storeName,
            reload: reload,
            resetQuery: resetQuery,
            openCreate: openCreate,
            openEdit: openEdit,
            save: save,
            openResetPwd: openResetPwd,
            doResetPwd: doResetPwd,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
