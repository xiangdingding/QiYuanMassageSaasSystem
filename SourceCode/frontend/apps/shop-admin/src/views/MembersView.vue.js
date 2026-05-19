import { onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import dayjs from 'dayjs';
import { membersApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
const appStore = useAppStore();
const rows = ref([]);
const total = ref(0);
const loading = ref(false);
const saving = ref(false);
const query = reactive({ page: 1, pageSize: 20, keyword: '', includeClosed: false });
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
    remark: '',
    referrerKeyword: '',
    referredByMemberId: null
});
const referrerCandidates = ref([]);
async function searchReferrer() {
    if (!form.referrerKeyword.trim())
        return;
    const r = await membersApi.list({
        keyword: form.referrerKeyword.trim(),
        page: 1, pageSize: 10,
        storeId: appStore.activeStoreId ?? undefined
    });
    referrerCandidates.value = r.items.filter((m) => m.isActive);
    if (!referrerCandidates.value.length)
        ElMessage.info('没有匹配的可用会员');
}
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
            storeId: appStore.activeStoreId ?? undefined,
            includeClosed: query.includeClosed
        });
        rows.value = data.items;
        total.value = data.total;
    }
    finally {
        loading.value = false;
    }
}
const RECHARGE_KIND_LABEL = {
    Recharge: '充值', Refund: '退卡', TransferOut: '转出', TransferIn: '转入', ReferralBonus: '返佣'
};
const RECHARGE_KIND_TAG = {
    Recharge: 'success', Refund: 'danger', TransferOut: 'warning', TransferIn: 'info', ReferralBonus: 'success'
};
function rechargeKindLabel(k) { return RECHARGE_KIND_LABEL[k] ?? k; }
function rechargeKindTag(k) { return RECHARGE_KIND_TAG[k] ?? 'info'; }
function resetQuery() {
    query.keyword = '';
    query.page = 1;
    reload();
}
function openCreate() {
    formMode.value = 'create';
    editingId.value = null;
    Object.assign(form, {
        cardNo: '', phone: '', name: '', gender: '', birthday: '',
        discount: 1, initialBalance: 0, remark: '',
        referrerKeyword: '', referredByMemberId: null
    });
    referrerCandidates.value = [];
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
        remark: row.remark ?? '',
        referrerKeyword: '',
        referredByMemberId: row.referredByMemberId ?? null
    });
    referrerCandidates.value = [];
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
                remark: form.remark || null,
                referredByMemberId: form.referredByMemberId
            });
        }
        else if (editingId.value != null) {
            await membersApi.update(editingId.value, {
                phone: form.phone,
                name: form.name || null,
                gender: form.gender || null,
                birthday: form.birthday || null,
                discount: form.discount,
                remark: form.remark || null,
                referredByMemberId: form.referredByMemberId
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
// ---- 退卡 ----
const refundOpen = ref(false);
const refundTarget = ref(null);
const rfForm = reactive({ refundAmount: 0, refundMethod: 'Cash', reason: '' });
function openRefund(row) {
    refundTarget.value = row;
    rfForm.refundAmount = row.balance;
    rfForm.refundMethod = 'Cash';
    rfForm.reason = '';
    refundOpen.value = true;
}
async function doRefund() {
    if (!refundTarget.value)
        return;
    if (rfForm.refundAmount <= 0) {
        ElMessage.warning('退款金额必须 > 0');
        return;
    }
    try {
        await ElMessageBox.confirm(`确认从「${refundTarget.value.name || refundTarget.value.cardNo}」退还 ¥${rfForm.refundAmount.toFixed(2)}，并关闭该卡？`, '退卡确认', { type: 'warning' });
    }
    catch {
        return;
    }
    saving.value = true;
    try {
        await membersApi.refund(refundTarget.value.id, {
            refundAmount: rfForm.refundAmount,
            refundMethod: rfForm.refundMethod,
            reason: rfForm.reason || null
        });
        ElMessage.success('已退卡');
        refundOpen.value = false;
        reload();
    }
    finally {
        saving.value = false;
    }
}
// ---- 转赠 ----
const transferOpen = ref(false);
const transferTarget = ref(null);
const tfForm = reactive({
    mode: 'existing',
    toQuery: '',
    toMemberId: null,
    newMemberCardNo: '',
    newMemberPhone: '',
    newMemberName: '',
    reason: ''
});
const targetCandidates = ref([]);
function openTransfer(row) {
    transferTarget.value = row;
    tfForm.mode = 'existing';
    tfForm.toQuery = '';
    tfForm.toMemberId = null;
    tfForm.newMemberCardNo = '';
    tfForm.newMemberPhone = '';
    tfForm.newMemberName = '';
    tfForm.reason = '';
    targetCandidates.value = [];
    transferOpen.value = true;
}
async function searchTarget() {
    if (!tfForm.toQuery.trim())
        return;
    const r = await membersApi.list({
        keyword: tfForm.toQuery.trim(),
        page: 1, pageSize: 10,
        storeId: appStore.activeStoreId ?? undefined
    });
    targetCandidates.value = r.items.filter((m) => m.id !== transferTarget.value?.id && m.isActive);
    if (!targetCandidates.value.length)
        ElMessage.info('没有匹配的可用会员');
}
async function doTransfer() {
    if (!transferTarget.value)
        return;
    if (tfForm.mode === 'existing' && !tfForm.toMemberId) {
        ElMessage.warning('请选择目标会员');
        return;
    }
    if (tfForm.mode === 'new' && (!tfForm.newMemberCardNo || !tfForm.newMemberPhone)) {
        ElMessage.warning('新会员卡号和手机号必填');
        return;
    }
    saving.value = true;
    try {
        await membersApi.transfer(transferTarget.value.id, {
            toMemberId: tfForm.mode === 'existing' ? tfForm.toMemberId : null,
            newMemberCardNo: tfForm.mode === 'new' ? tfForm.newMemberCardNo : null,
            newMemberPhone: tfForm.mode === 'new' ? tfForm.newMemberPhone : null,
            newMemberName: tfForm.mode === 'new' ? tfForm.newMemberName || null : null,
            reason: tfForm.reason || null
        });
        ElMessage.success('已转赠');
        transferOpen.value = false;
        reload();
    }
    finally {
        saving.value = false;
    }
}
// ---- 引荐 ----
const referralsOpen = ref(false);
const referralsData = ref(null);
async function openReferrals(row) {
    referralsData.value = await membersApi.referrals(row.id);
    referralsOpen.value = true;
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
    role: "search",
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
    'aria-label': "搜索会员，输入卡号、手机号或姓名后回车",
}));
const __VLS_6 = __VLS_5({
    ...{ 'onKeyup': {} },
    modelValue: (__VLS_ctx.query.keyword),
    placeholder: "卡号 / 手机号 / 姓名",
    clearable: true,
    ...{ style: {} },
    'aria-label': "搜索会员，输入卡号、手机号或姓名后回车",
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
    'aria-label': "执行会员搜索",
}));
const __VLS_14 = __VLS_13({
    ...{ 'onClick': {} },
    type: "primary",
    'aria-label': "执行会员搜索",
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
    'aria-label': "重置搜索条件",
}));
const __VLS_22 = __VLS_21({
    ...{ 'onClick': {} },
    'aria-label': "重置搜索条件",
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
    'aria-label': "开新会员卡",
}));
const __VLS_30 = __VLS_29({
    ...{ 'onClick': {} },
    type: "success",
    'aria-label': "开新会员卡",
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
let __VLS_32;
let __VLS_33;
let __VLS_34;
const __VLS_35 = {
    onClick: (__VLS_ctx.openCreate)
};
__VLS_31.slots.default;
var __VLS_31;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "toolbar" },
    ...{ style: {} },
});
const __VLS_36 = {}.ElCheckbox;
/** @type {[typeof __VLS_components.ElCheckbox, typeof __VLS_components.elCheckbox, typeof __VLS_components.ElCheckbox, typeof __VLS_components.elCheckbox, ]} */ ;
// @ts-ignore
const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.query.includeClosed),
}));
const __VLS_38 = __VLS_37({
    ...{ 'onChange': {} },
    modelValue: (__VLS_ctx.query.includeClosed),
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
let __VLS_40;
let __VLS_41;
let __VLS_42;
const __VLS_43 = {
    onChange: (__VLS_ctx.reload)
};
__VLS_39.slots.default;
var __VLS_39;
const __VLS_44 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_46 = __VLS_45({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_47.slots.default;
const __VLS_48 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
    prop: "cardNo",
    label: "卡号",
    width: "120",
}));
const __VLS_50 = __VLS_49({
    prop: "cardNo",
    label: "卡号",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_49));
__VLS_51.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_51.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
        ...{ class: ({ closed: !row.isActive }) },
    });
    (row.cardNo);
    if (!row.isActive) {
        const __VLS_52 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
            size: "small",
            type: "info",
            ...{ style: {} },
        }));
        const __VLS_54 = __VLS_53({
            size: "small",
            type: "info",
            ...{ style: {} },
        }, ...__VLS_functionalComponentArgsRest(__VLS_53));
        __VLS_55.slots.default;
        var __VLS_55;
    }
}
var __VLS_51;
const __VLS_56 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
    prop: "name",
    label: "姓名",
    width: "100",
}));
const __VLS_58 = __VLS_57({
    prop: "name",
    label: "姓名",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_57));
const __VLS_60 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
    prop: "phone",
    label: "手机号",
    width: "130",
}));
const __VLS_62 = __VLS_61({
    prop: "phone",
    label: "手机号",
    width: "130",
}, ...__VLS_functionalComponentArgsRest(__VLS_61));
const __VLS_64 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
    label: "余额",
    width: "120",
}));
const __VLS_66 = __VLS_65({
    label: "余额",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_65));
__VLS_67.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_67.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    __VLS_asFunctionalElement(__VLS_intrinsicElements.strong, __VLS_intrinsicElements.strong)({
        ...{ style: {} },
    });
    (row.balance.toFixed(2));
}
var __VLS_67;
const __VLS_68 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
    label: "累计充值",
    width: "120",
}));
const __VLS_70 = __VLS_69({
    label: "累计充值",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_69));
__VLS_71.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_71.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.totalRecharge.toFixed(2));
}
var __VLS_71;
const __VLS_72 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
    label: "累计消费",
    width: "120",
}));
const __VLS_74 = __VLS_73({
    label: "累计消费",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_73));
__VLS_75.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_75.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.totalConsumed.toFixed(2));
}
var __VLS_75;
const __VLS_76 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
    label: "折扣",
    width: "80",
}));
const __VLS_78 = __VLS_77({
    label: "折扣",
    width: "80",
}, ...__VLS_functionalComponentArgsRest(__VLS_77));
__VLS_79.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_79.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    if (row.discount < 1) {
        const __VLS_80 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
            size: "small",
            type: "warning",
        }));
        const __VLS_82 = __VLS_81({
            size: "small",
            type: "warning",
        }, ...__VLS_functionalComponentArgsRest(__VLS_81));
        __VLS_83.slots.default;
        ((row.discount * 10).toFixed(1));
        var __VLS_83;
    }
    else {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    }
}
var __VLS_79;
const __VLS_84 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_85 = __VLS_asFunctionalComponent(__VLS_84, new __VLS_84({
    label: "引荐人",
    width: "120",
}));
const __VLS_86 = __VLS_85({
    label: "引荐人",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_85));
__VLS_87.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_87.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    if (row.referredByMemberName) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
        (row.referredByMemberName);
    }
    else {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
            ...{ class: "muted" },
        });
    }
}
var __VLS_87;
const __VLS_88 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
    label: "操作",
    width: "380",
    fixed: "right",
}));
const __VLS_90 = __VLS_89({
    label: "操作",
    width: "380",
    fixed: "right",
}, ...__VLS_functionalComponentArgsRest(__VLS_89));
__VLS_91.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_91.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_92 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_93 = __VLS_asFunctionalComponent(__VLS_92, new __VLS_92({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
        disabled: (!row.isActive),
        'aria-label': (`给 ${row.name || row.cardNo} 充值`),
    }));
    const __VLS_94 = __VLS_93({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
        disabled: (!row.isActive),
        'aria-label': (`给 ${row.name || row.cardNo} 充值`),
    }, ...__VLS_functionalComponentArgsRest(__VLS_93));
    let __VLS_96;
    let __VLS_97;
    let __VLS_98;
    const __VLS_99 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openRecharge(row);
        }
    };
    __VLS_95.slots.default;
    var __VLS_95;
    const __VLS_100 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_101 = __VLS_asFunctionalComponent(__VLS_100, new __VLS_100({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
        'aria-label': (`查看 ${row.name || row.cardNo} 的流水`),
    }));
    const __VLS_102 = __VLS_101({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
        'aria-label': (`查看 ${row.name || row.cardNo} 的流水`),
    }, ...__VLS_functionalComponentArgsRest(__VLS_101));
    let __VLS_104;
    let __VLS_105;
    let __VLS_106;
    const __VLS_107 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openHistory(row);
        }
    };
    __VLS_103.slots.default;
    var __VLS_103;
    const __VLS_108 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_109 = __VLS_asFunctionalComponent(__VLS_108, new __VLS_108({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
        'aria-label': (`编辑 ${row.name || row.cardNo}`),
    }));
    const __VLS_110 = __VLS_109({
        ...{ 'onClick': {} },
        link: true,
        type: "primary",
        'aria-label': (`编辑 ${row.name || row.cardNo}`),
    }, ...__VLS_functionalComponentArgsRest(__VLS_109));
    let __VLS_112;
    let __VLS_113;
    let __VLS_114;
    const __VLS_115 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openEdit(row);
        }
    };
    __VLS_111.slots.default;
    var __VLS_111;
    const __VLS_116 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_117 = __VLS_asFunctionalComponent(__VLS_116, new __VLS_116({
        ...{ 'onClick': {} },
        link: true,
        type: "warning",
        disabled: (!row.isActive || row.balance <= 0),
        'aria-label': (`给 ${row.name || row.cardNo} 退卡`),
    }));
    const __VLS_118 = __VLS_117({
        ...{ 'onClick': {} },
        link: true,
        type: "warning",
        disabled: (!row.isActive || row.balance <= 0),
        'aria-label': (`给 ${row.name || row.cardNo} 退卡`),
    }, ...__VLS_functionalComponentArgsRest(__VLS_117));
    let __VLS_120;
    let __VLS_121;
    let __VLS_122;
    const __VLS_123 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openRefund(row);
        }
    };
    __VLS_119.slots.default;
    var __VLS_119;
    const __VLS_124 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_125 = __VLS_asFunctionalComponent(__VLS_124, new __VLS_124({
        ...{ 'onClick': {} },
        link: true,
        type: "warning",
        disabled: (!row.isActive || row.balance <= 0),
        'aria-label': (`把 ${row.name || row.cardNo} 余额转赠`),
    }));
    const __VLS_126 = __VLS_125({
        ...{ 'onClick': {} },
        link: true,
        type: "warning",
        disabled: (!row.isActive || row.balance <= 0),
        'aria-label': (`把 ${row.name || row.cardNo} 余额转赠`),
    }, ...__VLS_functionalComponentArgsRest(__VLS_125));
    let __VLS_128;
    let __VLS_129;
    let __VLS_130;
    const __VLS_131 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openTransfer(row);
        }
    };
    __VLS_127.slots.default;
    var __VLS_127;
    const __VLS_132 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_133 = __VLS_asFunctionalComponent(__VLS_132, new __VLS_132({
        ...{ 'onClick': {} },
        link: true,
        type: "info",
        'aria-label': (`查看 ${row.name || row.cardNo} 的引荐返佣`),
    }));
    const __VLS_134 = __VLS_133({
        ...{ 'onClick': {} },
        link: true,
        type: "info",
        'aria-label': (`查看 ${row.name || row.cardNo} 的引荐返佣`),
    }, ...__VLS_functionalComponentArgsRest(__VLS_133));
    let __VLS_136;
    let __VLS_137;
    let __VLS_138;
    const __VLS_139 = {
        onClick: (...[$event]) => {
            __VLS_ctx.openReferrals(row);
        }
    };
    __VLS_135.slots.default;
    var __VLS_135;
}
var __VLS_91;
var __VLS_47;
const __VLS_140 = {}.ElPagination;
/** @type {[typeof __VLS_components.ElPagination, typeof __VLS_components.elPagination, ]} */ ;
// @ts-ignore
const __VLS_141 = __VLS_asFunctionalComponent(__VLS_140, new __VLS_140({
    ...{ 'onCurrentChange': {} },
    ...{ 'onSizeChange': {} },
    ...{ style: {} },
    currentPage: (__VLS_ctx.query.page),
    pageSize: (__VLS_ctx.query.pageSize),
    total: (__VLS_ctx.total),
    pageSizes: ([10, 20, 50]),
    layout: "total, sizes, prev, pager, next, jumper",
}));
const __VLS_142 = __VLS_141({
    ...{ 'onCurrentChange': {} },
    ...{ 'onSizeChange': {} },
    ...{ style: {} },
    currentPage: (__VLS_ctx.query.page),
    pageSize: (__VLS_ctx.query.pageSize),
    total: (__VLS_ctx.total),
    pageSizes: ([10, 20, 50]),
    layout: "total, sizes, prev, pager, next, jumper",
}, ...__VLS_functionalComponentArgsRest(__VLS_141));
let __VLS_144;
let __VLS_145;
let __VLS_146;
const __VLS_147 = {
    onCurrentChange: ((p) => { __VLS_ctx.query.page = p; __VLS_ctx.reload(); })
};
const __VLS_148 = {
    onSizeChange: ((s) => { __VLS_ctx.query.pageSize = s; __VLS_ctx.query.page = 1; __VLS_ctx.reload(); })
};
var __VLS_143;
var __VLS_3;
const __VLS_149 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_150 = __VLS_asFunctionalComponent(__VLS_149, new __VLS_149({
    modelValue: (__VLS_ctx.formOpen),
    title: (__VLS_ctx.formMode === 'create' ? '开卡' : '编辑会员'),
    width: "480px",
}));
const __VLS_151 = __VLS_150({
    modelValue: (__VLS_ctx.formOpen),
    title: (__VLS_ctx.formMode === 'create' ? '开卡' : '编辑会员'),
    width: "480px",
}, ...__VLS_functionalComponentArgsRest(__VLS_150));
__VLS_152.slots.default;
const __VLS_153 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_154 = __VLS_asFunctionalComponent(__VLS_153, new __VLS_153({
    model: (__VLS_ctx.form),
    rules: (__VLS_ctx.rules),
    ref: "formRef",
    labelWidth: "100px",
}));
const __VLS_155 = __VLS_154({
    model: (__VLS_ctx.form),
    rules: (__VLS_ctx.rules),
    ref: "formRef",
    labelWidth: "100px",
}, ...__VLS_functionalComponentArgsRest(__VLS_154));
/** @type {typeof __VLS_ctx.formRef} */ ;
var __VLS_157 = {};
__VLS_156.slots.default;
const __VLS_159 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_160 = __VLS_asFunctionalComponent(__VLS_159, new __VLS_159({
    label: "卡号",
    prop: "cardNo",
}));
const __VLS_161 = __VLS_160({
    label: "卡号",
    prop: "cardNo",
}, ...__VLS_functionalComponentArgsRest(__VLS_160));
__VLS_162.slots.default;
const __VLS_163 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_164 = __VLS_asFunctionalComponent(__VLS_163, new __VLS_163({
    modelValue: (__VLS_ctx.form.cardNo),
    disabled: (__VLS_ctx.formMode === 'edit'),
}));
const __VLS_165 = __VLS_164({
    modelValue: (__VLS_ctx.form.cardNo),
    disabled: (__VLS_ctx.formMode === 'edit'),
}, ...__VLS_functionalComponentArgsRest(__VLS_164));
var __VLS_162;
const __VLS_167 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_168 = __VLS_asFunctionalComponent(__VLS_167, new __VLS_167({
    label: "手机号",
    prop: "phone",
}));
const __VLS_169 = __VLS_168({
    label: "手机号",
    prop: "phone",
}, ...__VLS_functionalComponentArgsRest(__VLS_168));
__VLS_170.slots.default;
const __VLS_171 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_172 = __VLS_asFunctionalComponent(__VLS_171, new __VLS_171({
    modelValue: (__VLS_ctx.form.phone),
}));
const __VLS_173 = __VLS_172({
    modelValue: (__VLS_ctx.form.phone),
}, ...__VLS_functionalComponentArgsRest(__VLS_172));
var __VLS_170;
const __VLS_175 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_176 = __VLS_asFunctionalComponent(__VLS_175, new __VLS_175({
    label: "姓名",
}));
const __VLS_177 = __VLS_176({
    label: "姓名",
}, ...__VLS_functionalComponentArgsRest(__VLS_176));
__VLS_178.slots.default;
const __VLS_179 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_180 = __VLS_asFunctionalComponent(__VLS_179, new __VLS_179({
    modelValue: (__VLS_ctx.form.name),
}));
const __VLS_181 = __VLS_180({
    modelValue: (__VLS_ctx.form.name),
}, ...__VLS_functionalComponentArgsRest(__VLS_180));
var __VLS_178;
const __VLS_183 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_184 = __VLS_asFunctionalComponent(__VLS_183, new __VLS_183({
    label: "性别",
}));
const __VLS_185 = __VLS_184({
    label: "性别",
}, ...__VLS_functionalComponentArgsRest(__VLS_184));
__VLS_186.slots.default;
const __VLS_187 = {}.ElRadioGroup;
/** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
// @ts-ignore
const __VLS_188 = __VLS_asFunctionalComponent(__VLS_187, new __VLS_187({
    modelValue: (__VLS_ctx.form.gender),
}));
const __VLS_189 = __VLS_188({
    modelValue: (__VLS_ctx.form.gender),
}, ...__VLS_functionalComponentArgsRest(__VLS_188));
__VLS_190.slots.default;
const __VLS_191 = {}.ElRadio;
/** @type {[typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, ]} */ ;
// @ts-ignore
const __VLS_192 = __VLS_asFunctionalComponent(__VLS_191, new __VLS_191({
    value: "男",
}));
const __VLS_193 = __VLS_192({
    value: "男",
}, ...__VLS_functionalComponentArgsRest(__VLS_192));
__VLS_194.slots.default;
var __VLS_194;
const __VLS_195 = {}.ElRadio;
/** @type {[typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, ]} */ ;
// @ts-ignore
const __VLS_196 = __VLS_asFunctionalComponent(__VLS_195, new __VLS_195({
    value: "女",
}));
const __VLS_197 = __VLS_196({
    value: "女",
}, ...__VLS_functionalComponentArgsRest(__VLS_196));
__VLS_198.slots.default;
var __VLS_198;
var __VLS_190;
var __VLS_186;
const __VLS_199 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_200 = __VLS_asFunctionalComponent(__VLS_199, new __VLS_199({
    label: "生日",
}));
const __VLS_201 = __VLS_200({
    label: "生日",
}, ...__VLS_functionalComponentArgsRest(__VLS_200));
__VLS_202.slots.default;
const __VLS_203 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_204 = __VLS_asFunctionalComponent(__VLS_203, new __VLS_203({
    modelValue: (__VLS_ctx.form.birthday),
    type: "date",
    placeholder: "可选",
    valueFormat: "YYYY-MM-DD",
}));
const __VLS_205 = __VLS_204({
    modelValue: (__VLS_ctx.form.birthday),
    type: "date",
    placeholder: "可选",
    valueFormat: "YYYY-MM-DD",
}, ...__VLS_functionalComponentArgsRest(__VLS_204));
var __VLS_202;
const __VLS_207 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_208 = __VLS_asFunctionalComponent(__VLS_207, new __VLS_207({
    label: "折扣",
    prop: "discount",
}));
const __VLS_209 = __VLS_208({
    label: "折扣",
    prop: "discount",
}, ...__VLS_functionalComponentArgsRest(__VLS_208));
__VLS_210.slots.default;
const __VLS_211 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_212 = __VLS_asFunctionalComponent(__VLS_211, new __VLS_211({
    modelValue: (__VLS_ctx.form.discount),
    min: (0.1),
    max: (1),
    step: (0.05),
    precision: (2),
}));
const __VLS_213 = __VLS_212({
    modelValue: (__VLS_ctx.form.discount),
    min: (0.1),
    max: (1),
    step: (0.05),
    precision: (2),
}, ...__VLS_functionalComponentArgsRest(__VLS_212));
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ class: "muted" },
    ...{ style: {} },
});
var __VLS_210;
if (__VLS_ctx.formMode === 'create') {
    const __VLS_215 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_216 = __VLS_asFunctionalComponent(__VLS_215, new __VLS_215({
        label: "初始充值",
    }));
    const __VLS_217 = __VLS_216({
        label: "初始充值",
    }, ...__VLS_functionalComponentArgsRest(__VLS_216));
    __VLS_218.slots.default;
    const __VLS_219 = {}.ElInputNumber;
    /** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
    // @ts-ignore
    const __VLS_220 = __VLS_asFunctionalComponent(__VLS_219, new __VLS_219({
        modelValue: (__VLS_ctx.form.initialBalance),
        min: (0),
        precision: (2),
        step: (100),
    }));
    const __VLS_221 = __VLS_220({
        modelValue: (__VLS_ctx.form.initialBalance),
        min: (0),
        precision: (2),
        step: (100),
    }, ...__VLS_functionalComponentArgsRest(__VLS_220));
    var __VLS_218;
}
const __VLS_223 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_224 = __VLS_asFunctionalComponent(__VLS_223, new __VLS_223({
    label: "引荐人",
}));
const __VLS_225 = __VLS_224({
    label: "引荐人",
}, ...__VLS_functionalComponentArgsRest(__VLS_224));
__VLS_226.slots.default;
const __VLS_227 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_228 = __VLS_asFunctionalComponent(__VLS_227, new __VLS_227({
    ...{ 'onKeyup': {} },
    modelValue: (__VLS_ctx.form.referrerKeyword),
    placeholder: "卡号 / 手机号 搜索后选择",
}));
const __VLS_229 = __VLS_228({
    ...{ 'onKeyup': {} },
    modelValue: (__VLS_ctx.form.referrerKeyword),
    placeholder: "卡号 / 手机号 搜索后选择",
}, ...__VLS_functionalComponentArgsRest(__VLS_228));
let __VLS_231;
let __VLS_232;
let __VLS_233;
const __VLS_234 = {
    onKeyup: (__VLS_ctx.searchReferrer)
};
var __VLS_230;
const __VLS_235 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_236 = __VLS_asFunctionalComponent(__VLS_235, new __VLS_235({
    ...{ 'onClick': {} },
    link: true,
    size: "small",
}));
const __VLS_237 = __VLS_236({
    ...{ 'onClick': {} },
    link: true,
    size: "small",
}, ...__VLS_functionalComponentArgsRest(__VLS_236));
let __VLS_239;
let __VLS_240;
let __VLS_241;
const __VLS_242 = {
    onClick: (__VLS_ctx.searchReferrer)
};
__VLS_238.slots.default;
var __VLS_238;
if (__VLS_ctx.referrerCandidates.length) {
    const __VLS_243 = {}.ElRadioGroup;
    /** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
    // @ts-ignore
    const __VLS_244 = __VLS_asFunctionalComponent(__VLS_243, new __VLS_243({
        modelValue: (__VLS_ctx.form.referredByMemberId),
        ...{ style: {} },
    }));
    const __VLS_245 = __VLS_244({
        modelValue: (__VLS_ctx.form.referredByMemberId),
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_244));
    __VLS_246.slots.default;
    for (const [c] of __VLS_getVForSourceType((__VLS_ctx.referrerCandidates))) {
        const __VLS_247 = {}.ElRadio;
        /** @type {[typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, ]} */ ;
        // @ts-ignore
        const __VLS_248 = __VLS_asFunctionalComponent(__VLS_247, new __VLS_247({
            key: (c.id),
            value: (c.id),
        }));
        const __VLS_249 = __VLS_248({
            key: (c.id),
            value: (c.id),
        }, ...__VLS_functionalComponentArgsRest(__VLS_248));
        __VLS_250.slots.default;
        (c.cardNo);
        (c.name || '未填');
        (c.phone);
        var __VLS_250;
    }
    var __VLS_246;
}
if (__VLS_ctx.form.referredByMemberId) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
        ...{ class: "muted" },
    });
    (__VLS_ctx.form.referredByMemberId);
    const __VLS_251 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_252 = __VLS_asFunctionalComponent(__VLS_251, new __VLS_251({
        ...{ 'onClick': {} },
        link: true,
        type: "danger",
        size: "small",
    }));
    const __VLS_253 = __VLS_252({
        ...{ 'onClick': {} },
        link: true,
        type: "danger",
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_252));
    let __VLS_255;
    let __VLS_256;
    let __VLS_257;
    const __VLS_258 = {
        onClick: (...[$event]) => {
            if (!(__VLS_ctx.form.referredByMemberId))
                return;
            __VLS_ctx.form.referredByMemberId = null;
        }
    };
    __VLS_254.slots.default;
    var __VLS_254;
}
var __VLS_226;
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
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
}));
const __VLS_265 = __VLS_264({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
}, ...__VLS_functionalComponentArgsRest(__VLS_264));
var __VLS_262;
var __VLS_156;
{
    const { footer: __VLS_thisSlot } = __VLS_152.slots;
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
            __VLS_ctx.formOpen = false;
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
        onClick: (__VLS_ctx.saveForm)
    };
    __VLS_278.slots.default;
    var __VLS_278;
}
var __VLS_152;
const __VLS_283 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_284 = __VLS_asFunctionalComponent(__VLS_283, new __VLS_283({
    modelValue: (__VLS_ctx.rechargeOpen),
    title: (`充值：${__VLS_ctx.rechargeTarget?.cardNo}`),
    width: "420px",
}));
const __VLS_285 = __VLS_284({
    modelValue: (__VLS_ctx.rechargeOpen),
    title: (`充值：${__VLS_ctx.rechargeTarget?.cardNo}`),
    width: "420px",
}, ...__VLS_functionalComponentArgsRest(__VLS_284));
__VLS_286.slots.default;
const __VLS_287 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_288 = __VLS_asFunctionalComponent(__VLS_287, new __VLS_287({
    model: (__VLS_ctx.rcForm),
    labelWidth: "100px",
}));
const __VLS_289 = __VLS_288({
    model: (__VLS_ctx.rcForm),
    labelWidth: "100px",
}, ...__VLS_functionalComponentArgsRest(__VLS_288));
__VLS_290.slots.default;
const __VLS_291 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_292 = __VLS_asFunctionalComponent(__VLS_291, new __VLS_291({
    label: "当前余额",
}));
const __VLS_293 = __VLS_292({
    label: "当前余额",
}, ...__VLS_functionalComponentArgsRest(__VLS_292));
__VLS_294.slots.default;
(__VLS_ctx.rechargeTarget?.balance.toFixed(2) ?? '0.00');
var __VLS_294;
const __VLS_295 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_296 = __VLS_asFunctionalComponent(__VLS_295, new __VLS_295({
    label: "充值金额",
}));
const __VLS_297 = __VLS_296({
    label: "充值金额",
}, ...__VLS_functionalComponentArgsRest(__VLS_296));
__VLS_298.slots.default;
const __VLS_299 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_300 = __VLS_asFunctionalComponent(__VLS_299, new __VLS_299({
    modelValue: (__VLS_ctx.rcForm.amount),
    min: (0),
    precision: (2),
    step: (100),
}));
const __VLS_301 = __VLS_300({
    modelValue: (__VLS_ctx.rcForm.amount),
    min: (0),
    precision: (2),
    step: (100),
}, ...__VLS_functionalComponentArgsRest(__VLS_300));
var __VLS_298;
const __VLS_303 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_304 = __VLS_asFunctionalComponent(__VLS_303, new __VLS_303({
    label: "赠送金额",
}));
const __VLS_305 = __VLS_304({
    label: "赠送金额",
}, ...__VLS_functionalComponentArgsRest(__VLS_304));
__VLS_306.slots.default;
const __VLS_307 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_308 = __VLS_asFunctionalComponent(__VLS_307, new __VLS_307({
    modelValue: (__VLS_ctx.rcForm.bonusAmount),
    min: (0),
    precision: (2),
    step: (50),
}));
const __VLS_309 = __VLS_308({
    modelValue: (__VLS_ctx.rcForm.bonusAmount),
    min: (0),
    precision: (2),
    step: (50),
}, ...__VLS_functionalComponentArgsRest(__VLS_308));
var __VLS_306;
const __VLS_311 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_312 = __VLS_asFunctionalComponent(__VLS_311, new __VLS_311({
    label: "支付方式",
}));
const __VLS_313 = __VLS_312({
    label: "支付方式",
}, ...__VLS_functionalComponentArgsRest(__VLS_312));
__VLS_314.slots.default;
const __VLS_315 = {}.ElRadioGroup;
/** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
// @ts-ignore
const __VLS_316 = __VLS_asFunctionalComponent(__VLS_315, new __VLS_315({
    modelValue: (__VLS_ctx.rcForm.payMethod),
}));
const __VLS_317 = __VLS_316({
    modelValue: (__VLS_ctx.rcForm.payMethod),
}, ...__VLS_functionalComponentArgsRest(__VLS_316));
__VLS_318.slots.default;
const __VLS_319 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_320 = __VLS_asFunctionalComponent(__VLS_319, new __VLS_319({
    value: "Cash",
}));
const __VLS_321 = __VLS_320({
    value: "Cash",
}, ...__VLS_functionalComponentArgsRest(__VLS_320));
__VLS_322.slots.default;
var __VLS_322;
const __VLS_323 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_324 = __VLS_asFunctionalComponent(__VLS_323, new __VLS_323({
    value: "Wechat",
}));
const __VLS_325 = __VLS_324({
    value: "Wechat",
}, ...__VLS_functionalComponentArgsRest(__VLS_324));
__VLS_326.slots.default;
var __VLS_326;
const __VLS_327 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_328 = __VLS_asFunctionalComponent(__VLS_327, new __VLS_327({
    value: "Alipay",
}));
const __VLS_329 = __VLS_328({
    value: "Alipay",
}, ...__VLS_functionalComponentArgsRest(__VLS_328));
__VLS_330.slots.default;
var __VLS_330;
const __VLS_331 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_332 = __VLS_asFunctionalComponent(__VLS_331, new __VLS_331({
    value: "BankCard",
}));
const __VLS_333 = __VLS_332({
    value: "BankCard",
}, ...__VLS_functionalComponentArgsRest(__VLS_332));
__VLS_334.slots.default;
var __VLS_334;
var __VLS_318;
var __VLS_314;
const __VLS_335 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_336 = __VLS_asFunctionalComponent(__VLS_335, new __VLS_335({
    label: "备注",
}));
const __VLS_337 = __VLS_336({
    label: "备注",
}, ...__VLS_functionalComponentArgsRest(__VLS_336));
__VLS_338.slots.default;
const __VLS_339 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_340 = __VLS_asFunctionalComponent(__VLS_339, new __VLS_339({
    modelValue: (__VLS_ctx.rcForm.remark),
    type: "textarea",
    rows: (2),
}));
const __VLS_341 = __VLS_340({
    modelValue: (__VLS_ctx.rcForm.remark),
    type: "textarea",
    rows: (2),
}, ...__VLS_functionalComponentArgsRest(__VLS_340));
var __VLS_338;
var __VLS_290;
{
    const { footer: __VLS_thisSlot } = __VLS_286.slots;
    const __VLS_343 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_344 = __VLS_asFunctionalComponent(__VLS_343, new __VLS_343({
        ...{ 'onClick': {} },
    }));
    const __VLS_345 = __VLS_344({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_344));
    let __VLS_347;
    let __VLS_348;
    let __VLS_349;
    const __VLS_350 = {
        onClick: (...[$event]) => {
            __VLS_ctx.rechargeOpen = false;
        }
    };
    __VLS_346.slots.default;
    var __VLS_346;
    const __VLS_351 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_352 = __VLS_asFunctionalComponent(__VLS_351, new __VLS_351({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_353 = __VLS_352({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_352));
    let __VLS_355;
    let __VLS_356;
    let __VLS_357;
    const __VLS_358 = {
        onClick: (__VLS_ctx.doRecharge)
    };
    __VLS_354.slots.default;
    var __VLS_354;
}
var __VLS_286;
const __VLS_359 = {}.ElDrawer;
/** @type {[typeof __VLS_components.ElDrawer, typeof __VLS_components.elDrawer, typeof __VLS_components.ElDrawer, typeof __VLS_components.elDrawer, ]} */ ;
// @ts-ignore
const __VLS_360 = __VLS_asFunctionalComponent(__VLS_359, new __VLS_359({
    modelValue: (__VLS_ctx.historyOpen),
    title: "会员流水",
    size: "640px",
}));
const __VLS_361 = __VLS_360({
    modelValue: (__VLS_ctx.historyOpen),
    title: "会员流水",
    size: "640px",
}, ...__VLS_functionalComponentArgsRest(__VLS_360));
__VLS_362.slots.default;
if (__VLS_ctx.historyTarget) {
    const __VLS_363 = {}.ElTabs;
    /** @type {[typeof __VLS_components.ElTabs, typeof __VLS_components.elTabs, typeof __VLS_components.ElTabs, typeof __VLS_components.elTabs, ]} */ ;
    // @ts-ignore
    const __VLS_364 = __VLS_asFunctionalComponent(__VLS_363, new __VLS_363({
        modelValue: (__VLS_ctx.historyTab),
    }));
    const __VLS_365 = __VLS_364({
        modelValue: (__VLS_ctx.historyTab),
    }, ...__VLS_functionalComponentArgsRest(__VLS_364));
    __VLS_366.slots.default;
    const __VLS_367 = {}.ElTabPane;
    /** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
    // @ts-ignore
    const __VLS_368 = __VLS_asFunctionalComponent(__VLS_367, new __VLS_367({
        label: "资金流水",
        name: "recharge",
    }));
    const __VLS_369 = __VLS_368({
        label: "资金流水",
        name: "recharge",
    }, ...__VLS_functionalComponentArgsRest(__VLS_368));
    __VLS_370.slots.default;
    const __VLS_371 = {}.ElTable;
    /** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
    // @ts-ignore
    const __VLS_372 = __VLS_asFunctionalComponent(__VLS_371, new __VLS_371({
        data: (__VLS_ctx.rechargeList),
        size: "small",
    }));
    const __VLS_373 = __VLS_372({
        data: (__VLS_ctx.rechargeList),
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_372));
    __VLS_374.slots.default;
    const __VLS_375 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_376 = __VLS_asFunctionalComponent(__VLS_375, new __VLS_375({
        label: "类型",
        width: "90",
    }));
    const __VLS_377 = __VLS_376({
        label: "类型",
        width: "90",
    }, ...__VLS_functionalComponentArgsRest(__VLS_376));
    __VLS_378.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_378.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        const __VLS_379 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_380 = __VLS_asFunctionalComponent(__VLS_379, new __VLS_379({
            size: "small",
            type: (__VLS_ctx.rechargeKindTag(row.kind)),
        }));
        const __VLS_381 = __VLS_380({
            size: "small",
            type: (__VLS_ctx.rechargeKindTag(row.kind)),
        }, ...__VLS_functionalComponentArgsRest(__VLS_380));
        __VLS_382.slots.default;
        (__VLS_ctx.rechargeKindLabel(row.kind));
        var __VLS_382;
    }
    var __VLS_378;
    const __VLS_383 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_384 = __VLS_asFunctionalComponent(__VLS_383, new __VLS_383({
        prop: "amount",
        label: "金额",
        width: "100",
    }));
    const __VLS_385 = __VLS_384({
        prop: "amount",
        label: "金额",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_384));
    __VLS_386.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_386.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.amount.toFixed(2));
    }
    var __VLS_386;
    const __VLS_387 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_388 = __VLS_asFunctionalComponent(__VLS_387, new __VLS_387({
        prop: "bonusAmount",
        label: "赠送",
        width: "80",
    }));
    const __VLS_389 = __VLS_388({
        prop: "bonusAmount",
        label: "赠送",
        width: "80",
    }, ...__VLS_functionalComponentArgsRest(__VLS_388));
    __VLS_390.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_390.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.bonusAmount.toFixed(2));
    }
    var __VLS_390;
    const __VLS_391 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_392 = __VLS_asFunctionalComponent(__VLS_391, new __VLS_391({
        prop: "balanceAfter",
        label: "后余额",
        width: "100",
    }));
    const __VLS_393 = __VLS_392({
        prop: "balanceAfter",
        label: "后余额",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_392));
    __VLS_394.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_394.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.balanceAfter.toFixed(2));
    }
    var __VLS_394;
    const __VLS_395 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_396 = __VLS_asFunctionalComponent(__VLS_395, new __VLS_395({
        prop: "payMethod",
        label: "支付",
        width: "80",
    }));
    const __VLS_397 = __VLS_396({
        prop: "payMethod",
        label: "支付",
        width: "80",
    }, ...__VLS_functionalComponentArgsRest(__VLS_396));
    const __VLS_399 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_400 = __VLS_asFunctionalComponent(__VLS_399, new __VLS_399({
        label: "对手会员",
        width: "120",
    }));
    const __VLS_401 = __VLS_400({
        label: "对手会员",
        width: "120",
    }, ...__VLS_functionalComponentArgsRest(__VLS_400));
    __VLS_402.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_402.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.counterpartyMemberName || '—');
    }
    var __VLS_402;
    const __VLS_403 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_404 = __VLS_asFunctionalComponent(__VLS_403, new __VLS_403({
        label: "时间",
        minWidth: "140",
    }));
    const __VLS_405 = __VLS_404({
        label: "时间",
        minWidth: "140",
    }, ...__VLS_functionalComponentArgsRest(__VLS_404));
    __VLS_406.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_406.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (__VLS_ctx.dayjs(row.createdAt).format('YYYY-MM-DD HH:mm'));
    }
    var __VLS_406;
    var __VLS_374;
    var __VLS_370;
    const __VLS_407 = {}.ElTabPane;
    /** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
    // @ts-ignore
    const __VLS_408 = __VLS_asFunctionalComponent(__VLS_407, new __VLS_407({
        label: "消费记录",
        name: "consume",
    }));
    const __VLS_409 = __VLS_408({
        label: "消费记录",
        name: "consume",
    }, ...__VLS_functionalComponentArgsRest(__VLS_408));
    __VLS_410.slots.default;
    const __VLS_411 = {}.ElTable;
    /** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
    // @ts-ignore
    const __VLS_412 = __VLS_asFunctionalComponent(__VLS_411, new __VLS_411({
        data: (__VLS_ctx.orderList),
        size: "small",
    }));
    const __VLS_413 = __VLS_412({
        data: (__VLS_ctx.orderList),
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_412));
    __VLS_414.slots.default;
    const __VLS_415 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_416 = __VLS_asFunctionalComponent(__VLS_415, new __VLS_415({
        prop: "orderNo",
        label: "订单",
        minWidth: "160",
    }));
    const __VLS_417 = __VLS_416({
        prop: "orderNo",
        label: "订单",
        minWidth: "160",
    }, ...__VLS_functionalComponentArgsRest(__VLS_416));
    const __VLS_419 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_420 = __VLS_asFunctionalComponent(__VLS_419, new __VLS_419({
        prop: "paidAmount",
        label: "实收",
        width: "100",
    }));
    const __VLS_421 = __VLS_420({
        prop: "paidAmount",
        label: "实收",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_420));
    __VLS_422.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_422.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.paidAmount.toFixed(2));
    }
    var __VLS_422;
    const __VLS_423 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_424 = __VLS_asFunctionalComponent(__VLS_423, new __VLS_423({
        prop: "status",
        label: "状态",
        width: "80",
    }));
    const __VLS_425 = __VLS_424({
        prop: "status",
        label: "状态",
        width: "80",
    }, ...__VLS_functionalComponentArgsRest(__VLS_424));
    const __VLS_427 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_428 = __VLS_asFunctionalComponent(__VLS_427, new __VLS_427({
        label: "时间",
        minWidth: "140",
    }));
    const __VLS_429 = __VLS_428({
        label: "时间",
        minWidth: "140",
    }, ...__VLS_functionalComponentArgsRest(__VLS_428));
    __VLS_430.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_430.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (__VLS_ctx.dayjs(row.createdAt).format('YYYY-MM-DD HH:mm'));
    }
    var __VLS_430;
    var __VLS_414;
    var __VLS_410;
    var __VLS_366;
}
var __VLS_362;
const __VLS_431 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_432 = __VLS_asFunctionalComponent(__VLS_431, new __VLS_431({
    modelValue: (__VLS_ctx.refundOpen),
    title: (`退卡：${__VLS_ctx.refundTarget?.cardNo}`),
    width: "420px",
}));
const __VLS_433 = __VLS_432({
    modelValue: (__VLS_ctx.refundOpen),
    title: (`退卡：${__VLS_ctx.refundTarget?.cardNo}`),
    width: "420px",
}, ...__VLS_functionalComponentArgsRest(__VLS_432));
__VLS_434.slots.default;
const __VLS_435 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_436 = __VLS_asFunctionalComponent(__VLS_435, new __VLS_435({
    model: (__VLS_ctx.rfForm),
    labelWidth: "100px",
}));
const __VLS_437 = __VLS_436({
    model: (__VLS_ctx.rfForm),
    labelWidth: "100px",
}, ...__VLS_functionalComponentArgsRest(__VLS_436));
__VLS_438.slots.default;
const __VLS_439 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_440 = __VLS_asFunctionalComponent(__VLS_439, new __VLS_439({
    label: "当前余额",
}));
const __VLS_441 = __VLS_440({
    label: "当前余额",
}, ...__VLS_functionalComponentArgsRest(__VLS_440));
__VLS_442.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.strong, __VLS_intrinsicElements.strong)({
    ...{ style: {} },
});
(__VLS_ctx.refundTarget?.balance.toFixed(2) ?? '0.00');
var __VLS_442;
const __VLS_443 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_444 = __VLS_asFunctionalComponent(__VLS_443, new __VLS_443({
    label: "退款金额",
    required: true,
}));
const __VLS_445 = __VLS_444({
    label: "退款金额",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_444));
__VLS_446.slots.default;
const __VLS_447 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_448 = __VLS_asFunctionalComponent(__VLS_447, new __VLS_447({
    modelValue: (__VLS_ctx.rfForm.refundAmount),
    min: (0.01),
    max: (__VLS_ctx.refundTarget?.balance ?? 0),
    precision: (2),
    step: (50),
}));
const __VLS_449 = __VLS_448({
    modelValue: (__VLS_ctx.rfForm.refundAmount),
    min: (0.01),
    max: (__VLS_ctx.refundTarget?.balance ?? 0),
    precision: (2),
    step: (50),
}, ...__VLS_functionalComponentArgsRest(__VLS_448));
const __VLS_451 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_452 = __VLS_asFunctionalComponent(__VLS_451, new __VLS_451({
    ...{ 'onClick': {} },
    link: true,
    size: "small",
    ...{ style: {} },
}));
const __VLS_453 = __VLS_452({
    ...{ 'onClick': {} },
    link: true,
    size: "small",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_452));
let __VLS_455;
let __VLS_456;
let __VLS_457;
const __VLS_458 = {
    onClick: (...[$event]) => {
        __VLS_ctx.rfForm.refundAmount = __VLS_ctx.refundTarget?.balance ?? 0;
    }
};
__VLS_454.slots.default;
var __VLS_454;
var __VLS_446;
const __VLS_459 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_460 = __VLS_asFunctionalComponent(__VLS_459, new __VLS_459({
    label: "退款方式",
    required: true,
}));
const __VLS_461 = __VLS_460({
    label: "退款方式",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_460));
__VLS_462.slots.default;
const __VLS_463 = {}.ElRadioGroup;
/** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
// @ts-ignore
const __VLS_464 = __VLS_asFunctionalComponent(__VLS_463, new __VLS_463({
    modelValue: (__VLS_ctx.rfForm.refundMethod),
}));
const __VLS_465 = __VLS_464({
    modelValue: (__VLS_ctx.rfForm.refundMethod),
}, ...__VLS_functionalComponentArgsRest(__VLS_464));
__VLS_466.slots.default;
const __VLS_467 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_468 = __VLS_asFunctionalComponent(__VLS_467, new __VLS_467({
    value: "Cash",
}));
const __VLS_469 = __VLS_468({
    value: "Cash",
}, ...__VLS_functionalComponentArgsRest(__VLS_468));
__VLS_470.slots.default;
var __VLS_470;
const __VLS_471 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_472 = __VLS_asFunctionalComponent(__VLS_471, new __VLS_471({
    value: "Wechat",
}));
const __VLS_473 = __VLS_472({
    value: "Wechat",
}, ...__VLS_functionalComponentArgsRest(__VLS_472));
__VLS_474.slots.default;
var __VLS_474;
const __VLS_475 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_476 = __VLS_asFunctionalComponent(__VLS_475, new __VLS_475({
    value: "Alipay",
}));
const __VLS_477 = __VLS_476({
    value: "Alipay",
}, ...__VLS_functionalComponentArgsRest(__VLS_476));
__VLS_478.slots.default;
var __VLS_478;
const __VLS_479 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_480 = __VLS_asFunctionalComponent(__VLS_479, new __VLS_479({
    value: "BankCard",
}));
const __VLS_481 = __VLS_480({
    value: "BankCard",
}, ...__VLS_functionalComponentArgsRest(__VLS_480));
__VLS_482.slots.default;
var __VLS_482;
var __VLS_466;
var __VLS_462;
const __VLS_483 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_484 = __VLS_asFunctionalComponent(__VLS_483, new __VLS_483({
    label: "原因",
}));
const __VLS_485 = __VLS_484({
    label: "原因",
}, ...__VLS_functionalComponentArgsRest(__VLS_484));
__VLS_486.slots.default;
const __VLS_487 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_488 = __VLS_asFunctionalComponent(__VLS_487, new __VLS_487({
    modelValue: (__VLS_ctx.rfForm.reason),
    type: "textarea",
    rows: (2),
    maxlength: "200",
}));
const __VLS_489 = __VLS_488({
    modelValue: (__VLS_ctx.rfForm.reason),
    type: "textarea",
    rows: (2),
    maxlength: "200",
}, ...__VLS_functionalComponentArgsRest(__VLS_488));
var __VLS_486;
const __VLS_491 = {}.ElAlert;
/** @type {[typeof __VLS_components.ElAlert, typeof __VLS_components.elAlert, ]} */ ;
// @ts-ignore
const __VLS_492 = __VLS_asFunctionalComponent(__VLS_491, new __VLS_491({
    type: "warning",
    closable: (false),
    title: "退卡后会员卡将被关闭，不能再充值或消费。请确认与客户达成一致再操作。",
}));
const __VLS_493 = __VLS_492({
    type: "warning",
    closable: (false),
    title: "退卡后会员卡将被关闭，不能再充值或消费。请确认与客户达成一致再操作。",
}, ...__VLS_functionalComponentArgsRest(__VLS_492));
var __VLS_438;
{
    const { footer: __VLS_thisSlot } = __VLS_434.slots;
    const __VLS_495 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_496 = __VLS_asFunctionalComponent(__VLS_495, new __VLS_495({
        ...{ 'onClick': {} },
    }));
    const __VLS_497 = __VLS_496({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_496));
    let __VLS_499;
    let __VLS_500;
    let __VLS_501;
    const __VLS_502 = {
        onClick: (...[$event]) => {
            __VLS_ctx.refundOpen = false;
        }
    };
    __VLS_498.slots.default;
    var __VLS_498;
    const __VLS_503 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_504 = __VLS_asFunctionalComponent(__VLS_503, new __VLS_503({
        ...{ 'onClick': {} },
        type: "warning",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_505 = __VLS_504({
        ...{ 'onClick': {} },
        type: "warning",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_504));
    let __VLS_507;
    let __VLS_508;
    let __VLS_509;
    const __VLS_510 = {
        onClick: (__VLS_ctx.doRefund)
    };
    __VLS_506.slots.default;
    var __VLS_506;
}
var __VLS_434;
const __VLS_511 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_512 = __VLS_asFunctionalComponent(__VLS_511, new __VLS_511({
    modelValue: (__VLS_ctx.transferOpen),
    title: (`转赠：${__VLS_ctx.transferTarget?.cardNo}`),
    width: "460px",
}));
const __VLS_513 = __VLS_512({
    modelValue: (__VLS_ctx.transferOpen),
    title: (`转赠：${__VLS_ctx.transferTarget?.cardNo}`),
    width: "460px",
}, ...__VLS_functionalComponentArgsRest(__VLS_512));
__VLS_514.slots.default;
const __VLS_515 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_516 = __VLS_asFunctionalComponent(__VLS_515, new __VLS_515({
    model: (__VLS_ctx.tfForm),
    labelWidth: "100px",
}));
const __VLS_517 = __VLS_516({
    model: (__VLS_ctx.tfForm),
    labelWidth: "100px",
}, ...__VLS_functionalComponentArgsRest(__VLS_516));
__VLS_518.slots.default;
const __VLS_519 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_520 = __VLS_asFunctionalComponent(__VLS_519, new __VLS_519({
    label: "当前余额",
}));
const __VLS_521 = __VLS_520({
    label: "当前余额",
}, ...__VLS_functionalComponentArgsRest(__VLS_520));
__VLS_522.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.strong, __VLS_intrinsicElements.strong)({
    ...{ style: {} },
});
(__VLS_ctx.transferTarget?.balance.toFixed(2) ?? '0.00');
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ class: "muted" },
    ...{ style: {} },
});
var __VLS_522;
const __VLS_523 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_524 = __VLS_asFunctionalComponent(__VLS_523, new __VLS_523({
    label: "转赠对象",
}));
const __VLS_525 = __VLS_524({
    label: "转赠对象",
}, ...__VLS_functionalComponentArgsRest(__VLS_524));
__VLS_526.slots.default;
const __VLS_527 = {}.ElRadioGroup;
/** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
// @ts-ignore
const __VLS_528 = __VLS_asFunctionalComponent(__VLS_527, new __VLS_527({
    modelValue: (__VLS_ctx.tfForm.mode),
}));
const __VLS_529 = __VLS_528({
    modelValue: (__VLS_ctx.tfForm.mode),
}, ...__VLS_functionalComponentArgsRest(__VLS_528));
__VLS_530.slots.default;
const __VLS_531 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_532 = __VLS_asFunctionalComponent(__VLS_531, new __VLS_531({
    value: "existing",
}));
const __VLS_533 = __VLS_532({
    value: "existing",
}, ...__VLS_functionalComponentArgsRest(__VLS_532));
__VLS_534.slots.default;
var __VLS_534;
const __VLS_535 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_536 = __VLS_asFunctionalComponent(__VLS_535, new __VLS_535({
    value: "new",
}));
const __VLS_537 = __VLS_536({
    value: "new",
}, ...__VLS_functionalComponentArgsRest(__VLS_536));
__VLS_538.slots.default;
var __VLS_538;
var __VLS_530;
var __VLS_526;
if (__VLS_ctx.tfForm.mode === 'existing') {
    const __VLS_539 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_540 = __VLS_asFunctionalComponent(__VLS_539, new __VLS_539({
        label: "目标会员",
    }));
    const __VLS_541 = __VLS_540({
        label: "目标会员",
    }, ...__VLS_functionalComponentArgsRest(__VLS_540));
    __VLS_542.slots.default;
    const __VLS_543 = {}.ElInput;
    /** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
    // @ts-ignore
    const __VLS_544 = __VLS_asFunctionalComponent(__VLS_543, new __VLS_543({
        ...{ 'onKeyup': {} },
        modelValue: (__VLS_ctx.tfForm.toQuery),
        placeholder: "输入卡号 / 手机号搜索",
    }));
    const __VLS_545 = __VLS_544({
        ...{ 'onKeyup': {} },
        modelValue: (__VLS_ctx.tfForm.toQuery),
        placeholder: "输入卡号 / 手机号搜索",
    }, ...__VLS_functionalComponentArgsRest(__VLS_544));
    let __VLS_547;
    let __VLS_548;
    let __VLS_549;
    const __VLS_550 = {
        onKeyup: (__VLS_ctx.searchTarget)
    };
    var __VLS_546;
    const __VLS_551 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_552 = __VLS_asFunctionalComponent(__VLS_551, new __VLS_551({
        ...{ 'onClick': {} },
        link: true,
        size: "small",
    }));
    const __VLS_553 = __VLS_552({
        ...{ 'onClick': {} },
        link: true,
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_552));
    let __VLS_555;
    let __VLS_556;
    let __VLS_557;
    const __VLS_558 = {
        onClick: (__VLS_ctx.searchTarget)
    };
    __VLS_554.slots.default;
    var __VLS_554;
    if (__VLS_ctx.targetCandidates.length) {
        const __VLS_559 = {}.ElRadioGroup;
        /** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
        // @ts-ignore
        const __VLS_560 = __VLS_asFunctionalComponent(__VLS_559, new __VLS_559({
            modelValue: (__VLS_ctx.tfForm.toMemberId),
            ...{ style: {} },
        }));
        const __VLS_561 = __VLS_560({
            modelValue: (__VLS_ctx.tfForm.toMemberId),
            ...{ style: {} },
        }, ...__VLS_functionalComponentArgsRest(__VLS_560));
        __VLS_562.slots.default;
        for (const [c] of __VLS_getVForSourceType((__VLS_ctx.targetCandidates))) {
            const __VLS_563 = {}.ElRadio;
            /** @type {[typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, typeof __VLS_components.ElRadio, typeof __VLS_components.elRadio, ]} */ ;
            // @ts-ignore
            const __VLS_564 = __VLS_asFunctionalComponent(__VLS_563, new __VLS_563({
                key: (c.id),
                value: (c.id),
            }));
            const __VLS_565 = __VLS_564({
                key: (c.id),
                value: (c.id),
            }, ...__VLS_functionalComponentArgsRest(__VLS_564));
            __VLS_566.slots.default;
            (c.cardNo);
            (c.name || '未填');
            (c.phone);
            (c.balance.toFixed(2));
            var __VLS_566;
        }
        var __VLS_562;
    }
    var __VLS_542;
}
else {
    const __VLS_567 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_568 = __VLS_asFunctionalComponent(__VLS_567, new __VLS_567({
        label: "新会员卡号",
        required: true,
    }));
    const __VLS_569 = __VLS_568({
        label: "新会员卡号",
        required: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_568));
    __VLS_570.slots.default;
    const __VLS_571 = {}.ElInput;
    /** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
    // @ts-ignore
    const __VLS_572 = __VLS_asFunctionalComponent(__VLS_571, new __VLS_571({
        modelValue: (__VLS_ctx.tfForm.newMemberCardNo),
    }));
    const __VLS_573 = __VLS_572({
        modelValue: (__VLS_ctx.tfForm.newMemberCardNo),
    }, ...__VLS_functionalComponentArgsRest(__VLS_572));
    var __VLS_570;
    const __VLS_575 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_576 = __VLS_asFunctionalComponent(__VLS_575, new __VLS_575({
        label: "手机号",
        required: true,
    }));
    const __VLS_577 = __VLS_576({
        label: "手机号",
        required: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_576));
    __VLS_578.slots.default;
    const __VLS_579 = {}.ElInput;
    /** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
    // @ts-ignore
    const __VLS_580 = __VLS_asFunctionalComponent(__VLS_579, new __VLS_579({
        modelValue: (__VLS_ctx.tfForm.newMemberPhone),
    }));
    const __VLS_581 = __VLS_580({
        modelValue: (__VLS_ctx.tfForm.newMemberPhone),
    }, ...__VLS_functionalComponentArgsRest(__VLS_580));
    var __VLS_578;
    const __VLS_583 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_584 = __VLS_asFunctionalComponent(__VLS_583, new __VLS_583({
        label: "姓名",
    }));
    const __VLS_585 = __VLS_584({
        label: "姓名",
    }, ...__VLS_functionalComponentArgsRest(__VLS_584));
    __VLS_586.slots.default;
    const __VLS_587 = {}.ElInput;
    /** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
    // @ts-ignore
    const __VLS_588 = __VLS_asFunctionalComponent(__VLS_587, new __VLS_587({
        modelValue: (__VLS_ctx.tfForm.newMemberName),
    }));
    const __VLS_589 = __VLS_588({
        modelValue: (__VLS_ctx.tfForm.newMemberName),
    }, ...__VLS_functionalComponentArgsRest(__VLS_588));
    var __VLS_586;
}
const __VLS_591 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_592 = __VLS_asFunctionalComponent(__VLS_591, new __VLS_591({
    label: "原因",
}));
const __VLS_593 = __VLS_592({
    label: "原因",
}, ...__VLS_functionalComponentArgsRest(__VLS_592));
__VLS_594.slots.default;
const __VLS_595 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_596 = __VLS_asFunctionalComponent(__VLS_595, new __VLS_595({
    modelValue: (__VLS_ctx.tfForm.reason),
    type: "textarea",
    rows: (2),
    maxlength: "200",
    placeholder: "如：转赠给家人 / 卡号变更",
}));
const __VLS_597 = __VLS_596({
    modelValue: (__VLS_ctx.tfForm.reason),
    type: "textarea",
    rows: (2),
    maxlength: "200",
    placeholder: "如：转赠给家人 / 卡号变更",
}, ...__VLS_functionalComponentArgsRest(__VLS_596));
var __VLS_594;
const __VLS_599 = {}.ElAlert;
/** @type {[typeof __VLS_components.ElAlert, typeof __VLS_components.elAlert, ]} */ ;
// @ts-ignore
const __VLS_600 = __VLS_asFunctionalComponent(__VLS_599, new __VLS_599({
    type: "warning",
    closable: (false),
    title: "转赠后原卡余额清零并关闭，目标卡余额累加（不计入对方累计充值，不会因此升级）。",
}));
const __VLS_601 = __VLS_600({
    type: "warning",
    closable: (false),
    title: "转赠后原卡余额清零并关闭，目标卡余额累加（不计入对方累计充值，不会因此升级）。",
}, ...__VLS_functionalComponentArgsRest(__VLS_600));
var __VLS_518;
{
    const { footer: __VLS_thisSlot } = __VLS_514.slots;
    const __VLS_603 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_604 = __VLS_asFunctionalComponent(__VLS_603, new __VLS_603({
        ...{ 'onClick': {} },
    }));
    const __VLS_605 = __VLS_604({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_604));
    let __VLS_607;
    let __VLS_608;
    let __VLS_609;
    const __VLS_610 = {
        onClick: (...[$event]) => {
            __VLS_ctx.transferOpen = false;
        }
    };
    __VLS_606.slots.default;
    var __VLS_606;
    const __VLS_611 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_612 = __VLS_asFunctionalComponent(__VLS_611, new __VLS_611({
        ...{ 'onClick': {} },
        type: "warning",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_613 = __VLS_612({
        ...{ 'onClick': {} },
        type: "warning",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_612));
    let __VLS_615;
    let __VLS_616;
    let __VLS_617;
    const __VLS_618 = {
        onClick: (__VLS_ctx.doTransfer)
    };
    __VLS_614.slots.default;
    var __VLS_614;
}
var __VLS_514;
const __VLS_619 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_620 = __VLS_asFunctionalComponent(__VLS_619, new __VLS_619({
    modelValue: (__VLS_ctx.referralsOpen),
    title: (`${__VLS_ctx.referralsData?.referrerName} 的引荐情况`),
    width: "640px",
}));
const __VLS_621 = __VLS_620({
    modelValue: (__VLS_ctx.referralsOpen),
    title: (`${__VLS_ctx.referralsData?.referrerName} 的引荐情况`),
    width: "640px",
}, ...__VLS_functionalComponentArgsRest(__VLS_620));
__VLS_622.slots.default;
if (__VLS_ctx.referralsData) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({});
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "metric-row" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({});
    __VLS_asFunctionalElement(__VLS_intrinsicElements.strong, __VLS_intrinsicElements.strong)({});
    (__VLS_ctx.referralsData.referredCount);
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({});
    __VLS_asFunctionalElement(__VLS_intrinsicElements.strong, __VLS_intrinsicElements.strong)({
        ...{ style: {} },
    });
    (__VLS_ctx.referralsData.totalRewardEarned.toFixed(2));
    const __VLS_623 = {}.ElTable;
    /** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
    // @ts-ignore
    const __VLS_624 = __VLS_asFunctionalComponent(__VLS_623, new __VLS_623({
        data: (__VLS_ctx.referralsData.referredMembers),
        size: "small",
        stripe: true,
        ...{ style: {} },
    }));
    const __VLS_625 = __VLS_624({
        data: (__VLS_ctx.referralsData.referredMembers),
        size: "small",
        stripe: true,
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_624));
    __VLS_626.slots.default;
    const __VLS_627 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_628 = __VLS_asFunctionalComponent(__VLS_627, new __VLS_627({
        prop: "cardNo",
        label: "卡号",
        width: "120",
    }));
    const __VLS_629 = __VLS_628({
        prop: "cardNo",
        label: "卡号",
        width: "120",
    }, ...__VLS_functionalComponentArgsRest(__VLS_628));
    const __VLS_631 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_632 = __VLS_asFunctionalComponent(__VLS_631, new __VLS_631({
        prop: "name",
        label: "姓名",
        width: "100",
    }));
    const __VLS_633 = __VLS_632({
        prop: "name",
        label: "姓名",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_632));
    const __VLS_635 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_636 = __VLS_asFunctionalComponent(__VLS_635, new __VLS_635({
        prop: "phone",
        label: "手机号",
        width: "140",
    }));
    const __VLS_637 = __VLS_636({
        prop: "phone",
        label: "手机号",
        width: "140",
    }, ...__VLS_functionalComponentArgsRest(__VLS_636));
    const __VLS_639 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_640 = __VLS_asFunctionalComponent(__VLS_639, new __VLS_639({
        label: "累计充值",
        width: "120",
    }));
    const __VLS_641 = __VLS_640({
        label: "累计充值",
        width: "120",
    }, ...__VLS_functionalComponentArgsRest(__VLS_640));
    __VLS_642.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_642.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.totalRecharge.toFixed(2));
    }
    var __VLS_642;
    const __VLS_643 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_644 = __VLS_asFunctionalComponent(__VLS_643, new __VLS_643({
        label: "开卡时间",
        minWidth: "160",
    }));
    const __VLS_645 = __VLS_644({
        label: "开卡时间",
        minWidth: "160",
    }, ...__VLS_functionalComponentArgsRest(__VLS_644));
    __VLS_646.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_646.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (__VLS_ctx.dayjs(row.createdAt).format('YYYY-MM-DD HH:mm'));
    }
    var __VLS_646;
    var __VLS_626;
    if (__VLS_ctx.referralsData.referredCount === 0) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.p, __VLS_intrinsicElements.p)({
            ...{ class: "muted" },
            ...{ style: {} },
        });
    }
}
var __VLS_622;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['metric-row']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
// @ts-ignore
var __VLS_158 = __VLS_157;
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
            referrerCandidates: referrerCandidates,
            searchReferrer: searchReferrer,
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
            rechargeKindLabel: rechargeKindLabel,
            rechargeKindTag: rechargeKindTag,
            resetQuery: resetQuery,
            openCreate: openCreate,
            openEdit: openEdit,
            saveForm: saveForm,
            openRecharge: openRecharge,
            doRecharge: doRecharge,
            openHistory: openHistory,
            refundOpen: refundOpen,
            refundTarget: refundTarget,
            rfForm: rfForm,
            openRefund: openRefund,
            doRefund: doRefund,
            transferOpen: transferOpen,
            transferTarget: transferTarget,
            tfForm: tfForm,
            targetCandidates: targetCandidates,
            openTransfer: openTransfer,
            searchTarget: searchTarget,
            doTransfer: doTransfer,
            referralsOpen: referralsOpen,
            referralsData: referralsData,
            openReferrals: openReferrals,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
