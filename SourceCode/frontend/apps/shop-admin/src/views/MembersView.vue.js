import { onMounted, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import dayjs from 'dayjs';
import { membersApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
const appStore = useAppStore();
const rows = ref([]);
const total = ref(0);
const loading = ref(false);
const saving = ref(false);
const query = reactive({ page: 1, pageSize: 20, keyword: '' });
const formOpen = ref(false);
const formMode = ref('create');
const editingId = ref(null);
const formRef = ref();
const form = reactive({
    cardNo: '',
    phone: '',
    name: '',
    gender: '',
    birthday: '',
    discount: 1,
    initialBalance: 0,
    remark: ''
});
const rules = {
    cardNo: [{ required: true, message: '请输入卡号', trigger: 'blur' }],
    phone: [{ required: true, message: '请输入手机号', trigger: 'blur' }],
    discount: [{ required: true, message: '请输入折扣', trigger: 'blur' }]
};
const rechargeOpen = ref(false);
const rechargeTarget = ref(null);
const rcForm = reactive({ amount: 100, bonusAmount: 0, payMethod: 'Cash', remark: '' });
const historyOpen = ref(false);
const historyTarget = ref(null);
const historyTab = ref('recharge');
const rechargeList = ref([]);
const orderList = ref([]);
async function reload() {
    loading.value = true;
    try {
        const data = await membersApi.list({
            page: query.page,
            pageSize: query.pageSize,
            keyword: query.keyword || undefined,
            storeId: appStore.activeStoreId ?? undefined
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
    query.page = 1;
    reload();
}
function openCreate() {
    formMode.value = 'create';
    editingId.value = null;
    Object.assign(form, { cardNo: '', phone: '', name: '', gender: '', birthday: '', discount: 1, initialBalance: 0, remark: '' });
    formOpen.value = true;
}
function openEdit(row) {
    formMode.value = 'edit';
    editingId.value = row.id;
    Object.assign(form, {
        cardNo: row.cardNo,
        phone: row.phone,
        name: row.name ?? '',
        gender: row.gender ?? '',
        birthday: row.birthday ?? '',
        discount: row.discount,
        initialBalance: 0,
        remark: row.remark ?? ''
    });
    formOpen.value = true;
}
async function saveForm() {
    if (!formRef.value)
        return;
    const ok = await formRef.value.validate().catch(() => false);
    if (!ok)
        return;
    if (!appStore.activeStoreId) {
        ElMessage.error('未选择门店');
        return;
    }
    saving.value = true;
    try {
        if (formMode.value === 'create') {
            await membersApi.create({
                storeId: appStore.activeStoreId,
                cardNo: form.cardNo,
                phone: form.phone,
                name: form.name || null,
                gender: form.gender || null,
                birthday: form.birthday || null,
                discount: form.discount,
                initialBalance: form.initialBalance,
                remark: form.remark || null
            });
        }
        else if (editingId.value != null) {
            await membersApi.update(editingId.value, {
                phone: form.phone,
                name: form.name || null,
                gender: form.gender || null,
                birthday: form.birthday || null,
                discount: form.discount,
                remark: form.remark || null
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
function openRecharge(row) {
    rechargeTarget.value = row;
    Object.assign(rcForm, { amount: 100, bonusAmount: 0, payMethod: 'Cash', remark: '' });
    rechargeOpen.value = true;
}
async function doRecharge() {
    if (!rechargeTarget.value)
        return;
    if (rcForm.amount <= 0) {
        ElMessage.warning('充值金额必须 > 0');
        return;
    }
    saving.value = true;
    try {
        await membersApi.recharge({
            memberId: rechargeTarget.value.id,
            amount: rcForm.amount,
            bonusAmount: rcForm.bonusAmount,
            payMethod: rcForm.payMethod,
            remark: rcForm.remark || null
        });
        ElMessage.success(`充值成功，到账 ¥${(rcForm.amount + rcForm.bonusAmount).toFixed(2)}`);
        rechargeOpen.value = false;
        reload();
    }
    finally {
        saving.value = false;
    }
}
async function openHistory(row) {
    historyTarget.value = row;
    historyOpen.value = true;
    const [rs, os] = await Promise.all([
        membersApi.rechargeHistory(row.id),
        membersApi.consumptionHistory(row.id)
    ]);
    rechargeList.value = rs;
    orderList.value = os;
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
    placeholder: "卡号 / 手机号 / 姓名",
    clearable: true,
    ...{ style: {} },
}));
const __VLS_6 = __VLS_5({
    ...{ 'onKeyup': {} },
    modelValue: (__VLS_ctx.query.keyword),
    placeholder: "卡号 / 手机号 / 姓名",
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
    onClick: (__VLS_ctx.reload)
};
__VLS_15.slots.default;
var __VLS_15;
const __VLS_20 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
    ...{ 'onClick': {} },
}));
const __VLS_22 = __VLS_21({
    ...{ 'onClick': {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_21));
let __VLS_24;
let __VLS_25;
let __VLS_26;
const __VLS_27 = {
    onClick: (__VLS_ctx.resetQuery)
};
__VLS_23.slots.default;
var __VLS_23;
const __VLS_28 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
    ...{ 'onClick': {} },
    type: "success",
}));
const __VLS_30 = __VLS_29({
    ...{ 'onClick': {} },
    type: "success",
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
let __VLS_32;
let __VLS_33;
let __VLS_34;
const __VLS_35 = {
    onClick: (__VLS_ctx.openCreate)
};
__VLS_31.slots.default;
var __VLS_31;
const __VLS_36 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_38 = __VLS_37({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_39.slots.default;
const __VLS_40 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
    prop: "cardNo",
    label: "卡号",
    width: "120",
}));
const __VLS_42 = __VLS_41({
    prop: "cardNo",
    label: "卡号",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_41));
const __VLS_44 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    prop: "name",
    label: "姓名",
    width: "100",
}));
const __VLS_46 = __VLS_45({
    prop: "name",
    label: "姓名",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
const __VLS_48 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
    prop: "phone",
    label: "手机号",
    width: "130",
}));
const __VLS_50 = __VLS_49({
    prop: "phone",
    label: "手机号",
    width: "130",
}, ...__VLS_functionalComponentArgsRest(__VLS_49));
const __VLS_52 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
    label: "余额",
    width: "120",
}));
const __VLS_54 = __VLS_53({
    label: "余额",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_53));
__VLS_55.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_55.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    __VLS_asFunctionalElement(__VLS_intrinsicElements.strong, __VLS_intrinsicElements.strong)({
        ...{ style: {} },
    });
    (row.balance.toFixed(2));
}
var __VLS_55;
const __VLS_56 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
    label: "累计充值",
    width: "120",
}));
const __VLS_58 = __VLS_57({
    label: "累计充值",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_57));
__VLS_59.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_59.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.totalRecharge.toFixed(2));
}
var __VLS_59;
const __VLS_60 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
    label: "累计消费",
    width: "120",
}));
const __VLS_62 = __VLS_61({
    label: "累计消费",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_61));
__VLS_63.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_63.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.totalConsumed.toFixed(2));
}
var __VLS_63;
const __VLS_64 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
    label: "折扣",
    width: "80",
}));
const __VLS_66 = __VLS_65({
    label: "折扣",
    width: "80",
}, ...__VLS_functionalComponentArgsRest(__VLS_65));
__VLS_67.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_67.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    if (row.discount < 1) {
        const __VLS_68 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
            size: "small",
            type: "warning",
        }));
        const __VLS_70 = __VLS_69({
            size: "small",
            type: "warning",
        }, ...__VLS_functionalComponentArgsRest(__VLS_69));
        __VLS_71.slots.default;
        ((row.discount * 10).toFixed(1));
        var __VLS_71;
    }
    else {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    }
}
var __VLS_67;
const __VLS_72 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
    label: "操作",
    width: "220",
    fixed: "right",
}));
const __VLS_74 = __VLS_73({
    label: "操作",
    width: "220",
    fixed: "right",
}, ...__VLS_functionalComponentArgsRest(__VLS_73));
__VLS_75.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_75.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_76 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
    }));
    const __VLS_78 = __VLS_77({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
    }, ...__VLS_functionalComponentArgsRest(__VLS_77));
    let __VLS_80;
    let __VLS_81;
    let __VLS_82;
    const __VLS_83 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openRecharge(row);
        }
    };
    __VLS_79.slots.default;
    var __VLS_79;
    const __VLS_84 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_85 = __VLS_asFunctionalComponent(__VLS_84, new __VLS_84({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
    }));
    const __VLS_86 = __VLS_85({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
    }, ...__VLS_functionalComponentArgsRest(__VLS_85));
    let __VLS_88;
    let __VLS_89;
    let __VLS_90;
    const __VLS_91 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openHistory(row);
        }
    };
    __VLS_87.slots.default;
    var __VLS_87;
    const __VLS_92 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_93 = __VLS_asFunctionalComponent(__VLS_92, new __VLS_92({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
    }));
    const __VLS_94 = __VLS_93({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
    }, ...__VLS_functionalComponentArgsRest(__VLS_93));
    let __VLS_96;
    let __VLS_97;
    let __VLS_98;
    const __VLS_99 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openEdit(row);
        }
    };
    __VLS_95.slots.default;
    var __VLS_95;
}
var __VLS_75;
var __VLS_39;
const __VLS_100 = {}.ElPagination;
/** @type {[typeof __VLS_components.ElPagination, typeof __VLS_components.elPagination, ]} */ ;
// @ts-ignore
const __VLS_101 = __VLS_asFunctionalComponent(__VLS_100, new __VLS_100({
    ...{ 'onCurrentChange': {} },
    ...{ 'onSizeChange': {} },
    ...{ style: {} },
    currentPage: (__VLS_ctx.query.page),
    pageSize: (__VLS_ctx.query.pageSize),
    total: (__VLS_ctx.total),
    pageSizes: ([10, 20, 50]),
    layout: "total, sizes, prev, pager, next, jumper",
}));
const __VLS_102 = __VLS_101({
    ...{ 'onCurrentChange': {} },
    ...{ 'onSizeChange': {} },
    ...{ style: {} },
    currentPage: (__VLS_ctx.query.page),
    pageSize: (__VLS_ctx.query.pageSize),
    total: (__VLS_ctx.total),
    pageSizes: ([10, 20, 50]),
    layout: "total, sizes, prev, pager, next, jumper",
}, ...__VLS_functionalComponentArgsRest(__VLS_101));
let __VLS_104;
let __VLS_105;
let __VLS_106;
const __VLS_107 = {
    onCurrentChange: ((p) => { __VLS_ctx.query.page = p; __VLS_ctx.reload(); })
};
const __VLS_108 = {
    onSizeChange: ((s) => { __VLS_ctx.query.pageSize = s; __VLS_ctx.query.page = 1; __VLS_ctx.reload(); })
};
var __VLS_103;
var __VLS_3;
const __VLS_109 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_110 = __VLS_asFunctionalComponent(__VLS_109, new __VLS_109({
    modelValue: (__VLS_ctx.formOpen),
    title: (__VLS_ctx.formMode === 'create' ? '开卡' : '编辑会员'),
    width: "480px",
}));
const __VLS_111 = __VLS_110({
    modelValue: (__VLS_ctx.formOpen),
    title: (__VLS_ctx.formMode === 'create' ? '开卡' : '编辑会员'),
    width: "480px",
}, ...__VLS_functionalComponentArgsRest(__VLS_110));
__VLS_112.slots.default;
const __VLS_113 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_114 = __VLS_asFunctionalComponent(__VLS_113, new __VLS_113({
    model: (__VLS_ctx.form),
    rules: (__VLS_ctx.rules),
    ref: "formRef",
    labelWidth: "100px",
}));
const __VLS_115 = __VLS_114({
    model: (__VLS_ctx.form),
    rules: (__VLS_ctx.rules),
    ref: "formRef",
    labelWidth: "100px",
}, ...__VLS_functionalComponentArgsRest(__VLS_114));
/** @type {typeof __VLS_ctx.formRef} */ ;
var __VLS_117 = {};
__VLS_116.slots.default;
const __VLS_119 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_120 = __VLS_asFunctionalComponent(__VLS_119, new __VLS_119({
    label: "卡号",
    prop: "cardNo",
}));
const __VLS_121 = __VLS_120({
    label: "卡号",
    prop: "cardNo",
}, ...__VLS_functionalComponentArgsRest(__VLS_120));
__VLS_122.slots.default;
const __VLS_123 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_124 = __VLS_asFunctionalComponent(__VLS_123, new __VLS_123({
    modelValue: (__VLS_ctx.form.cardNo),
    disabled: (__VLS_ctx.formMode === 'edit'),
}));
const __VLS_125 = __VLS_124({
    modelValue: (__VLS_ctx.form.cardNo),
    disabled: (__VLS_ctx.formMode === 'edit'),
}, ...__VLS_functionalComponentArgsRest(__VLS_124));
var __VLS_122;
const __VLS_127 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_128 = __VLS_asFunctionalComponent(__VLS_127, new __VLS_127({
    label: "手机号",
    prop: "phone",
}));
const __VLS_129 = __VLS_128({
    label: "手机号",
    prop: "phone",
}, ...__VLS_functionalComponentArgsRest(__VLS_128));
__VLS_130.slots.default;
const __VLS_131 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_132 = __VLS_asFunctionalComponent(__VLS_131, new __VLS_131({
    modelValue: (__VLS_ctx.form.phone),
}));
const __VLS_133 = __VLS_132({
    modelValue: (__VLS_ctx.form.phone),
}, ...__VLS_functionalComponentArgsRest(__VLS_132));
var __VLS_130;
const __VLS_135 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_136 = __VLS_asFunctionalComponent(__VLS_135, new __VLS_135({
    label: "姓名",
}));
const __VLS_137 = __VLS_136({
    label: "姓名",
}, ...__VLS_functionalComponentArgsRest(__VLS_136));
__VLS_138.slots.default;
const __VLS_139 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_140 = __VLS_asFunctionalComponent(__VLS_139, new __VLS_139({
    modelValue: (__VLS_ctx.form.name),
}));
const __VLS_141 = __VLS_140({
    modelValue: (__VLS_ctx.form.name),
}, ...__VLS_functionalComponentArgsRest(__VLS_140));
var __VLS_138;
const __VLS_143 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_144 = __VLS_asFunctionalComponent(__VLS_143, new __VLS_143({
    label: "性别",
}));
const __VLS_145 = __VLS_144({
    label: "性别",
}, ...__VLS_functionalComponentArgsRest(__VLS_144));
__VLS_146.slots.default;
const __VLS_147 = {}.ElRadioGroup;
/** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
// @ts-ignore
const __VLS_148 = __VLS_asFunctionalComponent(__VLS_147, new __VLS_147({
    modelValue: (__VLS_ctx.form.gender),
}));
const __VLS_149 = __VLS_148({
    modelValue: (__VLS_ctx.form.gender),
}, ...__VLS_functionalComponentArgsRest(__VLS_148));
__VLS_150.slots.default;
const __VLS_151 = {}.ElRadio;
/** @type {[typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, ]} */ ;
// @ts-ignore
const __VLS_152 = __VLS_asFunctionalComponent(__VLS_151, new __VLS_151({
    value: "男",
}));
const __VLS_153 = __VLS_152({
    value: "男",
}, ...__VLS_functionalComponentArgsRest(__VLS_152));
__VLS_154.slots.default;
var __VLS_154;
const __VLS_155 = {}.ElRadio;
/** @type {[typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, ]} */ ;
// @ts-ignore
const __VLS_156 = __VLS_asFunctionalComponent(__VLS_155, new __VLS_155({
    value: "女",
}));
const __VLS_157 = __VLS_156({
    value: "女",
}, ...__VLS_functionalComponentArgsRest(__VLS_156));
__VLS_158.slots.default;
var __VLS_158;
var __VLS_150;
var __VLS_146;
const __VLS_159 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_160 = __VLS_asFunctionalComponent(__VLS_159, new __VLS_159({
    label: "生日",
}));
const __VLS_161 = __VLS_160({
    label: "生日",
}, ...__VLS_functionalComponentArgsRest(__VLS_160));
__VLS_162.slots.default;
const __VLS_163 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_164 = __VLS_asFunctionalComponent(__VLS_163, new __VLS_163({
    modelValue: (__VLS_ctx.form.birthday),
    type: "date",
    placeholder: "可选",
    valueFormat: "YYYY-MM-DD",
}));
const __VLS_165 = __VLS_164({
    modelValue: (__VLS_ctx.form.birthday),
    type: "date",
    placeholder: "可选",
    valueFormat: "YYYY-MM-DD",
}, ...__VLS_functionalComponentArgsRest(__VLS_164));
var __VLS_162;
const __VLS_167 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_168 = __VLS_asFunctionalComponent(__VLS_167, new __VLS_167({
    label: "折扣",
    prop: "discount",
}));
const __VLS_169 = __VLS_168({
    label: "折扣",
    prop: "discount",
}, ...__VLS_functionalComponentArgsRest(__VLS_168));
__VLS_170.slots.default;
const __VLS_171 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_172 = __VLS_asFunctionalComponent(__VLS_171, new __VLS_171({
    modelValue: (__VLS_ctx.form.discount),
    min: (0.1),
    max: (1),
    step: (0.05),
    precision: (2),
}));
const __VLS_173 = __VLS_172({
    modelValue: (__VLS_ctx.form.discount),
    min: (0.1),
    max: (1),
    step: (0.05),
    precision: (2),
}, ...__VLS_functionalComponentArgsRest(__VLS_172));
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ class: "muted" },
    ...{ style: {} },
});
var __VLS_170;
if (__VLS_ctx.formMode === 'create') {
    const __VLS_175 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_176 = __VLS_asFunctionalComponent(__VLS_175, new __VLS_175({
        label: "初始充值",
    }));
    const __VLS_177 = __VLS_176({
        label: "初始充值",
    }, ...__VLS_functionalComponentArgsRest(__VLS_176));
    __VLS_178.slots.default;
    const __VLS_179 = {}.ElInputNumber;
    /** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
    // @ts-ignore
    const __VLS_180 = __VLS_asFunctionalComponent(__VLS_179, new __VLS_179({
        modelValue: (__VLS_ctx.form.initialBalance),
        min: (0),
        precision: (2),
        step: (100),
    }));
    const __VLS_181 = __VLS_180({
        modelValue: (__VLS_ctx.form.initialBalance),
        min: (0),
        precision: (2),
        step: (100),
    }, ...__VLS_functionalComponentArgsRest(__VLS_180));
    var __VLS_178;
}
const __VLS_183 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_184 = __VLS_asFunctionalComponent(__VLS_183, new __VLS_183({
    label: "备注",
}));
const __VLS_185 = __VLS_184({
    label: "备注",
}, ...__VLS_functionalComponentArgsRest(__VLS_184));
__VLS_186.slots.default;
const __VLS_187 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_188 = __VLS_asFunctionalComponent(__VLS_187, new __VLS_187({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
}));
const __VLS_189 = __VLS_188({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
}, ...__VLS_functionalComponentArgsRest(__VLS_188));
var __VLS_186;
var __VLS_116;
{
    const { footer: __VLS_thisSlot } = __VLS_112.slots;
    const __VLS_191 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_192 = __VLS_asFunctionalComponent(__VLS_191, new __VLS_191({
        ...{ 'onClick': {} },
    }));
    const __VLS_193 = __VLS_192({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_192));
    let __VLS_195;
    let __VLS_196;
    let __VLS_197;
    const __VLS_198 = {
        onClick: (...[$event]) => {
            __VLS_ctx.formOpen = false;
        }
    };
    __VLS_194.slots.default;
    var __VLS_194;
    const __VLS_199 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_200 = __VLS_asFunctionalComponent(__VLS_199, new __VLS_199({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_201 = __VLS_200({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_200));
    let __VLS_203;
    let __VLS_204;
    let __VLS_205;
    const __VLS_206 = {
        onClick: (__VLS_ctx.saveForm)
    };
    __VLS_202.slots.default;
    var __VLS_202;
}
var __VLS_112;
const __VLS_207 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_208 = __VLS_asFunctionalComponent(__VLS_207, new __VLS_207({
    modelValue: (__VLS_ctx.rechargeOpen),
    title: (`充值：${__VLS_ctx.rechargeTarget?.cardNo}`),
    width: "420px",
}));
const __VLS_209 = __VLS_208({
    modelValue: (__VLS_ctx.rechargeOpen),
    title: (`充值：${__VLS_ctx.rechargeTarget?.cardNo}`),
    width: "420px",
}, ...__VLS_functionalComponentArgsRest(__VLS_208));
__VLS_210.slots.default;
const __VLS_211 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_212 = __VLS_asFunctionalComponent(__VLS_211, new __VLS_211({
    model: (__VLS_ctx.rcForm),
    labelWidth: "100px",
}));
const __VLS_213 = __VLS_212({
    model: (__VLS_ctx.rcForm),
    labelWidth: "100px",
}, ...__VLS_functionalComponentArgsRest(__VLS_212));
__VLS_214.slots.default;
const __VLS_215 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_216 = __VLS_asFunctionalComponent(__VLS_215, new __VLS_215({
    label: "当前余额",
}));
const __VLS_217 = __VLS_216({
    label: "当前余额",
}, ...__VLS_functionalComponentArgsRest(__VLS_216));
__VLS_218.slots.default;
(__VLS_ctx.rechargeTarget?.balance.toFixed(2) ?? '0.00');
var __VLS_218;
const __VLS_219 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_220 = __VLS_asFunctionalComponent(__VLS_219, new __VLS_219({
    label: "充值金额",
}));
const __VLS_221 = __VLS_220({
    label: "充值金额",
}, ...__VLS_functionalComponentArgsRest(__VLS_220));
__VLS_222.slots.default;
const __VLS_223 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_224 = __VLS_asFunctionalComponent(__VLS_223, new __VLS_223({
    modelValue: (__VLS_ctx.rcForm.amount),
    min: (0),
    precision: (2),
    step: (100),
}));
const __VLS_225 = __VLS_224({
    modelValue: (__VLS_ctx.rcForm.amount),
    min: (0),
    precision: (2),
    step: (100),
}, ...__VLS_functionalComponentArgsRest(__VLS_224));
var __VLS_222;
const __VLS_227 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_228 = __VLS_asFunctionalComponent(__VLS_227, new __VLS_227({
    label: "赠送金额",
}));
const __VLS_229 = __VLS_228({
    label: "赠送金额",
}, ...__VLS_functionalComponentArgsRest(__VLS_228));
__VLS_230.slots.default;
const __VLS_231 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_232 = __VLS_asFunctionalComponent(__VLS_231, new __VLS_231({
    modelValue: (__VLS_ctx.rcForm.bonusAmount),
    min: (0),
    precision: (2),
    step: (50),
}));
const __VLS_233 = __VLS_232({
    modelValue: (__VLS_ctx.rcForm.bonusAmount),
    min: (0),
    precision: (2),
    step: (50),
}, ...__VLS_functionalComponentArgsRest(__VLS_232));
var __VLS_230;
const __VLS_235 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_236 = __VLS_asFunctionalComponent(__VLS_235, new __VLS_235({
    label: "支付方式",
}));
const __VLS_237 = __VLS_236({
    label: "支付方式",
}, ...__VLS_functionalComponentArgsRest(__VLS_236));
__VLS_238.slots.default;
const __VLS_239 = {}.ElRadioGroup;
/** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
// @ts-ignore
const __VLS_240 = __VLS_asFunctionalComponent(__VLS_239, new __VLS_239({
    modelValue: (__VLS_ctx.rcForm.payMethod),
}));
const __VLS_241 = __VLS_240({
    modelValue: (__VLS_ctx.rcForm.payMethod),
}, ...__VLS_functionalComponentArgsRest(__VLS_240));
__VLS_242.slots.default;
const __VLS_243 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_244 = __VLS_asFunctionalComponent(__VLS_243, new __VLS_243({
    value: "Cash",
}));
const __VLS_245 = __VLS_244({
    value: "Cash",
}, ...__VLS_functionalComponentArgsRest(__VLS_244));
__VLS_246.slots.default;
var __VLS_246;
const __VLS_247 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_248 = __VLS_asFunctionalComponent(__VLS_247, new __VLS_247({
    value: "Wechat",
}));
const __VLS_249 = __VLS_248({
    value: "Wechat",
}, ...__VLS_functionalComponentArgsRest(__VLS_248));
__VLS_250.slots.default;
var __VLS_250;
const __VLS_251 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_252 = __VLS_asFunctionalComponent(__VLS_251, new __VLS_251({
    value: "Alipay",
}));
const __VLS_253 = __VLS_252({
    value: "Alipay",
}, ...__VLS_functionalComponentArgsRest(__VLS_252));
__VLS_254.slots.default;
var __VLS_254;
const __VLS_255 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_256 = __VLS_asFunctionalComponent(__VLS_255, new __VLS_255({
    value: "BankCard",
}));
const __VLS_257 = __VLS_256({
    value: "BankCard",
}, ...__VLS_functionalComponentArgsRest(__VLS_256));
__VLS_258.slots.default;
var __VLS_258;
var __VLS_242;
var __VLS_238;
const __VLS_259 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_260 = __VLS_asFunctionalComponent(__VLS_259, new __VLS_259({
    label: "备注",
}));
const __VLS_261 = __VLS_260({
    label: "备注",
}, ...__VLS_functionalComponentArgsRest(__VLS_260));
__VLS_262.slots.default;
const __VLS_263 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_264 = __VLS_asFunctionalComponent(__VLS_263, new __VLS_263({
    modelValue: (__VLS_ctx.rcForm.remark),
    type: "textarea",
    rows: (2),
}));
const __VLS_265 = __VLS_264({
    modelValue: (__VLS_ctx.rcForm.remark),
    type: "textarea",
    rows: (2),
}, ...__VLS_functionalComponentArgsRest(__VLS_264));
var __VLS_262;
var __VLS_214;
{
    const { footer: __VLS_thisSlot } = __VLS_210.slots;
    const __VLS_267 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_268 = __VLS_asFunctionalComponent(__VLS_267, new __VLS_267({
        ...{ 'onClick': {} },
    }));
    const __VLS_269 = __VLS_268({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_268));
    let __VLS_271;
    let __VLS_272;
    let __VLS_273;
    const __VLS_274 = {
        onClick: (...[$event]) => {
            __VLS_ctx.rechargeOpen = false;
        }
    };
    __VLS_270.slots.default;
    var __VLS_270;
    const __VLS_275 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_276 = __VLS_asFunctionalComponent(__VLS_275, new __VLS_275({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_277 = __VLS_276({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_276));
    let __VLS_279;
    let __VLS_280;
    let __VLS_281;
    const __VLS_282 = {
        onClick: (__VLS_ctx.doRecharge)
    };
    __VLS_278.slots.default;
    var __VLS_278;
}
var __VLS_210;
const __VLS_283 = {}.ElDrawer;
/** @type {[typeof __VLS_components.ElDrawer, typeof __VLS_components.elDrawer, typeof __VLS_components.ElDrawer, typeof __VLS_components.elDrawer, ]} */ ;
// @ts-ignore
const __VLS_284 = __VLS_asFunctionalComponent(__VLS_283, new __VLS_283({
    modelValue: (__VLS_ctx.historyOpen),
    title: "会员流水",
    size: "520px",
}));
const __VLS_285 = __VLS_284({
    modelValue: (__VLS_ctx.historyOpen),
    title: "会员流水",
    size: "520px",
}, ...__VLS_functionalComponentArgsRest(__VLS_284));
__VLS_286.slots.default;
if (__VLS_ctx.historyTarget) {
    const __VLS_287 = {}.ElTabs;
    /** @type {[typeof __VLS_components.ElTabs, typeof __VLS_components.elTabs, typeof __VLS_components.ElTabs, typeof __VLS_components.elTabs, ]} */ ;
    // @ts-ignore
    const __VLS_288 = __VLS_asFunctionalComponent(__VLS_287, new __VLS_287({
        modelValue: (__VLS_ctx.historyTab),
    }));
    const __VLS_289 = __VLS_288({
        modelValue: (__VLS_ctx.historyTab),
    }, ...__VLS_functionalComponentArgsRest(__VLS_288));
    __VLS_290.slots.default;
    const __VLS_291 = {}.ElTabPane;
    /** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
    // @ts-ignore
    const __VLS_292 = __VLS_asFunctionalComponent(__VLS_291, new __VLS_291({
        label: "充值记录",
        name: "recharge",
    }));
    const __VLS_293 = __VLS_292({
        label: "充值记录",
        name: "recharge",
    }, ...__VLS_functionalComponentArgsRest(__VLS_292));
    __VLS_294.slots.default;
    const __VLS_295 = {}.ElTable;
    /** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
    // @ts-ignore
    const __VLS_296 = __VLS_asFunctionalComponent(__VLS_295, new __VLS_295({
        data: (__VLS_ctx.rechargeList),
        size: "small",
    }));
    const __VLS_297 = __VLS_296({
        data: (__VLS_ctx.rechargeList),
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_296));
    __VLS_298.slots.default;
    const __VLS_299 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_300 = __VLS_asFunctionalComponent(__VLS_299, new __VLS_299({
        prop: "amount",
        label: "金额",
        width: "100",
    }));
    const __VLS_301 = __VLS_300({
        prop: "amount",
        label: "金额",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_300));
    __VLS_302.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_302.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.amount.toFixed(2));
    }
    var __VLS_302;
    const __VLS_303 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_304 = __VLS_asFunctionalComponent(__VLS_303, new __VLS_303({
        prop: "bonusAmount",
        label: "赠送",
        width: "80",
    }));
    const __VLS_305 = __VLS_304({
        prop: "bonusAmount",
        label: "赠送",
        width: "80",
    }, ...__VLS_functionalComponentArgsRest(__VLS_304));
    __VLS_306.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_306.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.bonusAmount.toFixed(2));
    }
    var __VLS_306;
    const __VLS_307 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_308 = __VLS_asFunctionalComponent(__VLS_307, new __VLS_307({
        prop: "balanceAfter",
        label: "充后余额",
        width: "100",
    }));
    const __VLS_309 = __VLS_308({
        prop: "balanceAfter",
        label: "充后余额",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_308));
    __VLS_310.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_310.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.balanceAfter.toFixed(2));
    }
    var __VLS_310;
    const __VLS_311 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_312 = __VLS_asFunctionalComponent(__VLS_311, new __VLS_311({
        prop: "payMethod",
        label: "支付",
        width: "80",
    }));
    const __VLS_313 = __VLS_312({
        prop: "payMethod",
        label: "支付",
        width: "80",
    }, ...__VLS_functionalComponentArgsRest(__VLS_312));
    const __VLS_315 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_316 = __VLS_asFunctionalComponent(__VLS_315, new __VLS_315({
        label: "时间",
        minWidth: "140",
    }));
    const __VLS_317 = __VLS_316({
        label: "时间",
        minWidth: "140",
    }, ...__VLS_functionalComponentArgsRest(__VLS_316));
    __VLS_318.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_318.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (__VLS_ctx.dayjs(row.createdAt).format('YYYY-MM-DD HH:mm'));
    }
    var __VLS_318;
    var __VLS_298;
    var __VLS_294;
    const __VLS_319 = {}.ElTabPane;
    /** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
    // @ts-ignore
    const __VLS_320 = __VLS_asFunctionalComponent(__VLS_319, new __VLS_319({
        label: "消费记录",
        name: "consume",
    }));
    const __VLS_321 = __VLS_320({
        label: "消费记录",
        name: "consume",
    }, ...__VLS_functionalComponentArgsRest(__VLS_320));
    __VLS_322.slots.default;
    const __VLS_323 = {}.ElTable;
    /** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
    // @ts-ignore
    const __VLS_324 = __VLS_asFunctionalComponent(__VLS_323, new __VLS_323({
        data: (__VLS_ctx.orderList),
        size: "small",
    }));
    const __VLS_325 = __VLS_324({
        data: (__VLS_ctx.orderList),
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_324));
    __VLS_326.slots.default;
    const __VLS_327 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_328 = __VLS_asFunctionalComponent(__VLS_327, new __VLS_327({
        prop: "orderNo",
        label: "订单",
        minWidth: "160",
    }));
    const __VLS_329 = __VLS_328({
        prop: "orderNo",
        label: "订单",
        minWidth: "160",
    }, ...__VLS_functionalComponentArgsRest(__VLS_328));
    const __VLS_331 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_332 = __VLS_asFunctionalComponent(__VLS_331, new __VLS_331({
        prop: "paidAmount",
        label: "实收",
        width: "100",
    }));
    const __VLS_333 = __VLS_332({
        prop: "paidAmount",
        label: "实收",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_332));
    __VLS_334.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_334.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.paidAmount.toFixed(2));
    }
    var __VLS_334;
    const __VLS_335 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_336 = __VLS_asFunctionalComponent(__VLS_335, new __VLS_335({
        prop: "status",
        label: "状态",
        width: "80",
    }));
    const __VLS_337 = __VLS_336({
        prop: "status",
        label: "状态",
        width: "80",
    }, ...__VLS_functionalComponentArgsRest(__VLS_336));
    const __VLS_339 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_340 = __VLS_asFunctionalComponent(__VLS_339, new __VLS_339({
        label: "时间",
        minWidth: "140",
    }));
    const __VLS_341 = __VLS_340({
        label: "时间",
        minWidth: "140",
    }, ...__VLS_functionalComponentArgsRest(__VLS_340));
    __VLS_342.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_342.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (__VLS_ctx.dayjs(row.createdAt).format('YYYY-MM-DD HH:mm'));
    }
    var __VLS_342;
    var __VLS_326;
    var __VLS_322;
    var __VLS_290;
}
var __VLS_286;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
// @ts-ignore
var __VLS_118 = __VLS_117;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            dayjs: dayjs,
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
            rechargeOpen: rechargeOpen,
            rechargeTarget: rechargeTarget,
            rcForm: rcForm,
            historyOpen: historyOpen,
            historyTarget: historyTarget,
            historyTab: historyTab,
            rechargeList: rechargeList,
            orderList: orderList,
            reload: reload,
            resetQuery: resetQuery,
            openCreate: openCreate,
            openEdit: openEdit,
            saveForm: saveForm,
            openRecharge: openRecharge,
            doRecharge: doRecharge,
            openHistory: openHistory,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
