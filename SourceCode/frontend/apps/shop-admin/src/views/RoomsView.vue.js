import { computed, onMounted, reactive, ref, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, Refresh } from '@element-plus/icons-vue';
import { roomsApi, timedRoomsApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import { useAuthStore } from '@/stores/auth';
const appStore = useAppStore();
const auth = useAuthStore();
const rows = ref([]);
const loading = ref(false);
const includeInactive = ref(false);
const formOpen = ref(false);
const saving = ref(false);
const form = reactive({
    id: null, roomNo: '', capacity: 1, roomType: null, remark: null, isActive: true, isTimedRoom: false, hourlyRate: 0
});
const timedSessions = ref([]);
function timedOpen(roomId) {
    return timedSessions.value.find((s) => s.roomId === roomId && s.status === 'Open');
}
function timedStatusLabel(s) {
    return { Open: '计时中', Settled: '已结算', Cancelled: '已作废' }[s] ?? s;
}
function payLabel(p) {
    return { Cash: '现金', Wechat: '微信', Alipay: '支付宝', MemberCard: '会员卡', BankCard: '银行卡', Unpaid: '未付' }[p] ?? p;
}
const canManage = computed(() => auth.role === 'ShopOwner' || auth.role === 'StoreManager');
const activeStoreName = computed(() => appStore.stores.find((s) => s.id === appStore.activeStoreId)?.name ?? '');
async function reload() {
    if (!appStore.activeStoreId)
        return;
    loading.value = true;
    try {
        const [roomList, sessions] = await Promise.all([
            roomsApi.list(appStore.activeStoreId, includeInactive.value),
            timedRoomsApi.sessions(appStore.activeStoreId)
        ]);
        rows.value = roomList;
        timedSessions.value = sessions;
    }
    finally {
        loading.value = false;
    }
}
function openNew() {
    Object.assign(form, { id: null, roomNo: '', capacity: 1, roomType: null, remark: null, isActive: true, isTimedRoom: false, hourlyRate: 0 });
    formOpen.value = true;
}
function openEdit(row) {
    Object.assign(form, {
        id: row.id, roomNo: row.roomNo, capacity: row.capacity,
        roomType: row.roomType ?? null, remark: row.remark ?? null, isActive: row.isActive,
        isTimedRoom: row.isTimedRoom, hourlyRate: row.hourlyRate
    });
    formOpen.value = true;
}
async function save() {
    if (!form.roomNo.trim()) {
        ElMessage.warning('房间号必填');
        return;
    }
    if (form.isTimedRoom && form.hourlyRate <= 0) {
        ElMessage.warning('计时房需设置大于 0 的小时单价');
        return;
    }
    saving.value = true;
    try {
        if (form.id) {
            await roomsApi.update(form.id, {
                roomNo: form.roomNo.trim(), capacity: form.capacity,
                roomType: form.roomType, remark: form.remark, isActive: form.isActive,
                isTimedRoom: form.isTimedRoom, hourlyRate: form.hourlyRate
            });
        }
        else {
            await roomsApi.create({
                storeId: appStore.activeStoreId,
                roomNo: form.roomNo.trim(), capacity: form.capacity,
                roomType: form.roomType, remark: form.remark,
                isTimedRoom: form.isTimedRoom, hourlyRate: form.hourlyRate
            });
        }
        formOpen.value = false;
        ElMessage.success('已保存');
        await reload();
    }
    catch {
        /* http 已弹错 */
    }
    finally {
        saving.value = false;
    }
}
// ---- 计时房操作 ----
const startOpen = ref(false);
const startTarget = ref(null);
const startForm = reactive({ customerName: '', remark: '' });
function openStart(row) {
    startTarget.value = row;
    startForm.customerName = '';
    startForm.remark = '';
    startOpen.value = true;
}
async function doStart() {
    if (!startTarget.value)
        return;
    saving.value = true;
    try {
        await timedRoomsApi.start(startTarget.value.id, {
            customerName: startForm.customerName.trim() || null,
            remark: startForm.remark.trim() || null
        });
        startOpen.value = false;
        ElMessage.success('已开始计时');
        await reload();
    }
    finally {
        saving.value = false;
    }
}
const stopOpen = ref(false);
const stopTarget = ref(null);
const stopSession = ref(null);
const stopPayMethod = ref('Cash');
const estimatedAmount = computed(() => {
    if (!stopSession.value)
        return 0;
    return (stopSession.value.elapsedMinutes / 60) * stopSession.value.hourlyRateSnapshot;
});
function openStop(row) {
    const s = timedOpen(row.id);
    if (!s)
        return;
    stopTarget.value = row;
    stopSession.value = s;
    stopPayMethod.value = 'Cash';
    stopOpen.value = true;
}
async function doStop() {
    if (!stopSession.value)
        return;
    saving.value = true;
    try {
        const settled = await timedRoomsApi.stop(stopSession.value.id, stopPayMethod.value);
        stopOpen.value = false;
        ElMessage.success(`已结算 ¥${settled.amount.toFixed(2)}`);
        await reload();
    }
    finally {
        saving.value = false;
    }
}
async function cancelSession(row) {
    await ElMessageBox.confirm(`确认作废 ${row.roomNo} 号房的计时记录？`, '提示', { type: 'warning' }).catch(() => null);
    await timedRoomsApi.cancel(row.id);
    ElMessage.success('已作废');
    await reload();
}
async function remove(row) {
    await ElMessageBox.confirm(`确认删除房间 ${row.roomNo}？`, '提示', { type: 'warning' }).catch(() => null);
    await roomsApi.remove(row.id);
    ElMessage.success('已删除');
    await reload();
}
watch(() => appStore.activeStoreId, () => reload());
onMounted(async () => {
    await appStore.loadStores();
    await reload();
});
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
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
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ class: "title" },
});
(__VLS_ctx.activeStoreName);
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
__VLS_asFunctionalElement(__VLS_intrinsicElements.div)({
    ...{ class: "spacer" },
});
if (__VLS_ctx.canManage) {
    const __VLS_12 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
        ...{ 'onClick': {} },
        type: "primary",
        icon: (__VLS_ctx.Plus),
    }));
    const __VLS_14 = __VLS_13({
        ...{ 'onClick': {} },
        type: "primary",
        icon: (__VLS_ctx.Plus),
    }, ...__VLS_functionalComponentArgsRest(__VLS_13));
    let __VLS_16;
    let __VLS_17;
    let __VLS_18;
    const __VLS_19 = {
        onClick: (__VLS_ctx.openNew)
    };
    __VLS_15.slots.default;
    var __VLS_15;
}
const __VLS_20 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}));
const __VLS_22 = __VLS_21({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}, ...__VLS_functionalComponentArgsRest(__VLS_21));
let __VLS_24;
let __VLS_25;
let __VLS_26;
const __VLS_27 = {
    onClick: (__VLS_ctx.reload)
};
__VLS_23.slots.default;
var __VLS_23;
const __VLS_28 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_30 = __VLS_29({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_31.slots.default;
const __VLS_32 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
    prop: "roomNo",
    label: "房间号",
    width: "100",
}));
const __VLS_34 = __VLS_33({
    prop: "roomNo",
    label: "房间号",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_33));
const __VLS_36 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
    prop: "roomType",
    label: "类型",
    width: "120",
}));
const __VLS_38 = __VLS_37({
    prop: "roomType",
    label: "类型",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
__VLS_39.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_39.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    if (row.isTimedRoom) {
        const __VLS_40 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
            type: "primary",
            size: "small",
        }));
        const __VLS_42 = __VLS_41({
            type: "primary",
            size: "small",
        }, ...__VLS_functionalComponentArgsRest(__VLS_41));
        __VLS_43.slots.default;
        var __VLS_43;
    }
    else {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
        (row.roomType || '—');
    }
}
var __VLS_39;
const __VLS_44 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    prop: "capacity",
    label: "容量",
    width: "70",
}));
const __VLS_46 = __VLS_45({
    prop: "capacity",
    label: "容量",
    width: "70",
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
const __VLS_48 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
    label: "计时单价",
    width: "100",
}));
const __VLS_50 = __VLS_49({
    label: "计时单价",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_49));
__VLS_51.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_51.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    if (row.isTimedRoom) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
        (row.hourlyRate.toFixed(2));
    }
    else {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    }
}
var __VLS_51;
const __VLS_52 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
    label: "状态",
    width: "120",
}));
const __VLS_54 = __VLS_53({
    label: "状态",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_53));
__VLS_55.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_55.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    if (!row.isActive) {
        const __VLS_56 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
            type: "info",
        }));
        const __VLS_58 = __VLS_57({
            type: "info",
        }, ...__VLS_functionalComponentArgsRest(__VLS_57));
        __VLS_59.slots.default;
        var __VLS_59;
    }
    else if (row.isTimedRoom && __VLS_ctx.timedOpen(row.id)) {
        const __VLS_60 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
            type: "warning",
        }));
        const __VLS_62 = __VLS_61({
            type: "warning",
        }, ...__VLS_functionalComponentArgsRest(__VLS_61));
        __VLS_63.slots.default;
        var __VLS_63;
    }
    else if (row.isOccupied) {
        const __VLS_64 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
            type: "warning",
        }));
        const __VLS_66 = __VLS_65({
            type: "warning",
        }, ...__VLS_functionalComponentArgsRest(__VLS_65));
        __VLS_67.slots.default;
        var __VLS_67;
    }
    else {
        const __VLS_68 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
            type: "success",
        }));
        const __VLS_70 = __VLS_69({
            type: "success",
        }, ...__VLS_functionalComponentArgsRest(__VLS_69));
        __VLS_71.slots.default;
        var __VLS_71;
    }
}
var __VLS_55;
const __VLS_72 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
    label: "占用 / 计时",
    minWidth: "200",
}));
const __VLS_74 = __VLS_73({
    label: "占用 / 计时",
    minWidth: "200",
}, ...__VLS_functionalComponentArgsRest(__VLS_73));
__VLS_75.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_75.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    if (row.isTimedRoom && __VLS_ctx.timedOpen(row.id)) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
        (__VLS_ctx.timedOpen(row.id).elapsedMinutes);
        (__VLS_ctx.timedOpen(row.id).customerName || __VLS_ctx.timedOpen(row.id).memberName || '散客');
    }
    else {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
        (row.occupiedByOrderNo || '—');
    }
}
var __VLS_75;
const __VLS_76 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
    label: "操作",
    width: "260",
    fixed: "right",
}));
const __VLS_78 = __VLS_77({
    label: "操作",
    width: "260",
    fixed: "right",
}, ...__VLS_functionalComponentArgsRest(__VLS_77));
__VLS_79.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_79.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    if (row.isTimedRoom && row.isActive) {
        if (!__VLS_ctx.timedOpen(row.id)) {
            const __VLS_80 = {}.ElButton;
            /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
            // @ts-ignore
            const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
                ...{ 'onClick': {} },
                size: "small",
                type: "success",
            }));
            const __VLS_82 = __VLS_81({
                ...{ 'onClick': {} },
                size: "small",
                type: "success",
            }, ...__VLS_functionalComponentArgsRest(__VLS_81));
            let __VLS_84;
            let __VLS_85;
            let __VLS_86;
            const __VLS_87 = {
                onClick: (...[$event]) => {
                    if (!(row.isTimedRoom && row.isActive))
                        return;
                    if (!(!__VLS_ctx.timedOpen(row.id)))
                        return;
                    __VLS_ctx.openStart(row);
                }
            };
            __VLS_83.slots.default;
            var __VLS_83;
        }
        else {
            const __VLS_88 = {}.ElButton;
            /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
            // @ts-ignore
            const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
                ...{ 'onClick': {} },
                size: "small",
                type: "warning",
            }));
            const __VLS_90 = __VLS_89({
                ...{ 'onClick': {} },
                size: "small",
                type: "warning",
            }, ...__VLS_functionalComponentArgsRest(__VLS_89));
            let __VLS_92;
            let __VLS_93;
            let __VLS_94;
            const __VLS_95 = {
                onClick: (...[$event]) => {
                    if (!(row.isTimedRoom && row.isActive))
                        return;
                    if (!!(!__VLS_ctx.timedOpen(row.id)))
                        return;
                    __VLS_ctx.openStop(row);
                }
            };
            __VLS_91.slots.default;
            var __VLS_91;
        }
    }
    if (__VLS_ctx.canManage) {
        const __VLS_96 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({
            ...{ 'onClick': {} },
            size: "small",
            'aria-label': (`编辑 ${row.roomNo} 号房`),
        }));
        const __VLS_98 = __VLS_97({
            ...{ 'onClick': {} },
            size: "small",
            'aria-label': (`编辑 ${row.roomNo} 号房`),
        }, ...__VLS_functionalComponentArgsRest(__VLS_97));
        let __VLS_100;
        let __VLS_101;
        let __VLS_102;
        const __VLS_103 = {
            onClick: (...[$event]) => {
                if (!(__VLS_ctx.canManage))
                    return;
                __VLS_ctx.openEdit(row);
            }
        };
        __VLS_99.slots.default;
        var __VLS_99;
    }
    if (__VLS_ctx.canManage) {
        const __VLS_104 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_105 = __VLS_asFunctionalComponent(__VLS_104, new __VLS_104({
            ...{ 'onClick': {} },
            size: "small",
            type: "danger",
            disabled: (row.isOccupied || !!__VLS_ctx.timedOpen(row.id)),
        }));
        const __VLS_106 = __VLS_105({
            ...{ 'onClick': {} },
            size: "small",
            type: "danger",
            disabled: (row.isOccupied || !!__VLS_ctx.timedOpen(row.id)),
        }, ...__VLS_functionalComponentArgsRest(__VLS_105));
        let __VLS_108;
        let __VLS_109;
        let __VLS_110;
        const __VLS_111 = {
            onClick: (...[$event]) => {
                if (!(__VLS_ctx.canManage))
                    return;
                __VLS_ctx.remove(row);
            }
        };
        __VLS_107.slots.default;
        var __VLS_107;
    }
}
var __VLS_79;
var __VLS_31;
var __VLS_3;
if (__VLS_ctx.timedSessions.length) {
    const __VLS_112 = {}.ElCard;
    /** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
    // @ts-ignore
    const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({
        shadow: "never",
        ...{ style: {} },
    }));
    const __VLS_114 = __VLS_113({
        shadow: "never",
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_113));
    __VLS_115.slots.default;
    {
        const { header: __VLS_thisSlot } = __VLS_115.slots;
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    }
    const __VLS_116 = {}.ElTable;
    /** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
    // @ts-ignore
    const __VLS_117 = __VLS_asFunctionalComponent(__VLS_116, new __VLS_116({
        data: (__VLS_ctx.timedSessions),
        size: "small",
        stripe: true,
    }));
    const __VLS_118 = __VLS_117({
        data: (__VLS_ctx.timedSessions),
        size: "small",
        stripe: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_117));
    __VLS_119.slots.default;
    const __VLS_120 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_121 = __VLS_asFunctionalComponent(__VLS_120, new __VLS_120({
        prop: "roomNo",
        label: "房间",
        width: "90",
    }));
    const __VLS_122 = __VLS_121({
        prop: "roomNo",
        label: "房间",
        width: "90",
    }, ...__VLS_functionalComponentArgsRest(__VLS_121));
    const __VLS_124 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_125 = __VLS_asFunctionalComponent(__VLS_124, new __VLS_124({
        label: "客户",
        width: "120",
    }));
    const __VLS_126 = __VLS_125({
        label: "客户",
        width: "120",
    }, ...__VLS_functionalComponentArgsRest(__VLS_125));
    __VLS_127.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_127.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.customerName || row.memberName || '散客');
    }
    var __VLS_127;
    const __VLS_128 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_129 = __VLS_asFunctionalComponent(__VLS_128, new __VLS_128({
        label: "时长",
        width: "100",
    }));
    const __VLS_130 = __VLS_129({
        label: "时长",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_129));
    __VLS_131.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_131.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.billedMinutes || row.elapsedMinutes);
    }
    var __VLS_131;
    const __VLS_132 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_133 = __VLS_asFunctionalComponent(__VLS_132, new __VLS_132({
        label: "金额",
        width: "100",
    }));
    const __VLS_134 = __VLS_133({
        label: "金额",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_133));
    __VLS_135.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_135.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (row.amount.toFixed(2));
    }
    var __VLS_135;
    const __VLS_136 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_137 = __VLS_asFunctionalComponent(__VLS_136, new __VLS_136({
        label: "支付",
        width: "90",
    }));
    const __VLS_138 = __VLS_137({
        label: "支付",
        width: "90",
    }, ...__VLS_functionalComponentArgsRest(__VLS_137));
    __VLS_139.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_139.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        (__VLS_ctx.payLabel(row.payMethod));
    }
    var __VLS_139;
    const __VLS_140 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_141 = __VLS_asFunctionalComponent(__VLS_140, new __VLS_140({
        label: "状态",
        width: "100",
    }));
    const __VLS_142 = __VLS_141({
        label: "状态",
        width: "100",
    }, ...__VLS_functionalComponentArgsRest(__VLS_141));
    __VLS_143.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_143.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        const __VLS_144 = {}.ElTag;
        /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
        // @ts-ignore
        const __VLS_145 = __VLS_asFunctionalComponent(__VLS_144, new __VLS_144({
            size: "small",
            type: (row.status === 'Open' ? 'warning' : row.status === 'Settled' ? 'success' : 'info'),
        }));
        const __VLS_146 = __VLS_145({
            size: "small",
            type: (row.status === 'Open' ? 'warning' : row.status === 'Settled' ? 'success' : 'info'),
        }, ...__VLS_functionalComponentArgsRest(__VLS_145));
        __VLS_147.slots.default;
        (__VLS_ctx.timedStatusLabel(row.status));
        var __VLS_147;
    }
    var __VLS_143;
    const __VLS_148 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_149 = __VLS_asFunctionalComponent(__VLS_148, new __VLS_148({
        label: "操作",
        width: "90",
        fixed: "right",
    }));
    const __VLS_150 = __VLS_149({
        label: "操作",
        width: "90",
        fixed: "right",
    }, ...__VLS_functionalComponentArgsRest(__VLS_149));
    __VLS_151.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_151.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        if (row.status === 'Open') {
            const __VLS_152 = {}.ElButton;
            /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
            // @ts-ignore
            const __VLS_153 = __VLS_asFunctionalComponent(__VLS_152, new __VLS_152({
                ...{ 'onClick': {} },
                link: true,
                type: "danger",
                size: "small",
            }));
            const __VLS_154 = __VLS_153({
                ...{ 'onClick': {} },
                link: true,
                type: "danger",
                size: "small",
            }, ...__VLS_functionalComponentArgsRest(__VLS_153));
            let __VLS_156;
            let __VLS_157;
            let __VLS_158;
            const __VLS_159 = {
                onClick: (...[$event]) => {
                    if (!(__VLS_ctx.timedSessions.length))
                        return;
                    if (!(row.status === 'Open'))
                        return;
                    __VLS_ctx.cancelSession(row);
                }
            };
            __VLS_155.slots.default;
            var __VLS_155;
        }
    }
    var __VLS_151;
    var __VLS_119;
    var __VLS_115;
}
const __VLS_160 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_161 = __VLS_asFunctionalComponent(__VLS_160, new __VLS_160({
    modelValue: (__VLS_ctx.formOpen),
    title: (__VLS_ctx.form.id ? '编辑房间' : '新建房间'),
    width: "420px",
}));
const __VLS_162 = __VLS_161({
    modelValue: (__VLS_ctx.formOpen),
    title: (__VLS_ctx.form.id ? '编辑房间' : '新建房间'),
    width: "420px",
}, ...__VLS_functionalComponentArgsRest(__VLS_161));
__VLS_163.slots.default;
const __VLS_164 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_165 = __VLS_asFunctionalComponent(__VLS_164, new __VLS_164({
    model: (__VLS_ctx.form),
    labelWidth: "80px",
}));
const __VLS_166 = __VLS_165({
    model: (__VLS_ctx.form),
    labelWidth: "80px",
}, ...__VLS_functionalComponentArgsRest(__VLS_165));
__VLS_167.slots.default;
const __VLS_168 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_169 = __VLS_asFunctionalComponent(__VLS_168, new __VLS_168({
    label: "房间号",
    required: true,
}));
const __VLS_170 = __VLS_169({
    label: "房间号",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_169));
__VLS_171.slots.default;
const __VLS_172 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_173 = __VLS_asFunctionalComponent(__VLS_172, new __VLS_172({
    modelValue: (__VLS_ctx.form.roomNo),
    maxlength: "32",
    placeholder: "如 01 / VIP-1",
}));
const __VLS_174 = __VLS_173({
    modelValue: (__VLS_ctx.form.roomNo),
    maxlength: "32",
    placeholder: "如 01 / VIP-1",
}, ...__VLS_functionalComponentArgsRest(__VLS_173));
var __VLS_171;
const __VLS_176 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_177 = __VLS_asFunctionalComponent(__VLS_176, new __VLS_176({
    label: "容量",
    required: true,
}));
const __VLS_178 = __VLS_177({
    label: "容量",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_177));
__VLS_179.slots.default;
const __VLS_180 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_181 = __VLS_asFunctionalComponent(__VLS_180, new __VLS_180({
    modelValue: (__VLS_ctx.form.capacity),
    min: (1),
    max: (20),
}));
const __VLS_182 = __VLS_181({
    modelValue: (__VLS_ctx.form.capacity),
    min: (1),
    max: (20),
}, ...__VLS_functionalComponentArgsRest(__VLS_181));
var __VLS_179;
const __VLS_184 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_185 = __VLS_asFunctionalComponent(__VLS_184, new __VLS_184({
    label: "类型",
}));
const __VLS_186 = __VLS_185({
    label: "类型",
}, ...__VLS_functionalComponentArgsRest(__VLS_185));
__VLS_187.slots.default;
const __VLS_188 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_189 = __VLS_asFunctionalComponent(__VLS_188, new __VLS_188({
    modelValue: (__VLS_ctx.form.roomType),
    placeholder: "可选",
    clearable: true,
    filterable: true,
    allowCreate: true,
    ...{ style: {} },
}));
const __VLS_190 = __VLS_189({
    modelValue: (__VLS_ctx.form.roomType),
    placeholder: "可选",
    clearable: true,
    filterable: true,
    allowCreate: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_189));
__VLS_191.slots.default;
const __VLS_192 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_193 = __VLS_asFunctionalComponent(__VLS_192, new __VLS_192({
    label: "标准间",
    value: "standard",
}));
const __VLS_194 = __VLS_193({
    label: "标准间",
    value: "standard",
}, ...__VLS_functionalComponentArgsRest(__VLS_193));
const __VLS_196 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_197 = __VLS_asFunctionalComponent(__VLS_196, new __VLS_196({
    label: "VIP",
    value: "vip",
}));
const __VLS_198 = __VLS_197({
    label: "VIP",
    value: "vip",
}, ...__VLS_functionalComponentArgsRest(__VLS_197));
const __VLS_200 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_201 = __VLS_asFunctionalComponent(__VLS_200, new __VLS_200({
    label: "情侣间",
    value: "couple",
}));
const __VLS_202 = __VLS_201({
    label: "情侣间",
    value: "couple",
}, ...__VLS_functionalComponentArgsRest(__VLS_201));
var __VLS_191;
var __VLS_187;
const __VLS_204 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_205 = __VLS_asFunctionalComponent(__VLS_204, new __VLS_204({
    label: "计时房",
}));
const __VLS_206 = __VLS_205({
    label: "计时房",
}, ...__VLS_functionalComponentArgsRest(__VLS_205));
__VLS_207.slots.default;
const __VLS_208 = {}.ElSwitch;
/** @type {[typeof __VLS_components.ElSwitch, typeof __VLS_components.elSwitch, ]} */ ;
// @ts-ignore
const __VLS_209 = __VLS_asFunctionalComponent(__VLS_208, new __VLS_208({
    modelValue: (__VLS_ctx.form.isTimedRoom),
}));
const __VLS_210 = __VLS_209({
    modelValue: (__VLS_ctx.form.isTimedRoom),
}, ...__VLS_functionalComponentArgsRest(__VLS_209));
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ class: "hint" },
});
var __VLS_207;
if (__VLS_ctx.form.isTimedRoom) {
    const __VLS_212 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_213 = __VLS_asFunctionalComponent(__VLS_212, new __VLS_212({
        label: "小时单价",
        required: true,
    }));
    const __VLS_214 = __VLS_213({
        label: "小时单价",
        required: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_213));
    __VLS_215.slots.default;
    const __VLS_216 = {}.ElInputNumber;
    /** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
    // @ts-ignore
    const __VLS_217 = __VLS_asFunctionalComponent(__VLS_216, new __VLS_216({
        modelValue: (__VLS_ctx.form.hourlyRate),
        min: (0),
        precision: (2),
        step: (10),
    }));
    const __VLS_218 = __VLS_217({
        modelValue: (__VLS_ctx.form.hourlyRate),
        min: (0),
        precision: (2),
        step: (10),
    }, ...__VLS_functionalComponentArgsRest(__VLS_217));
    var __VLS_215;
}
const __VLS_220 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_221 = __VLS_asFunctionalComponent(__VLS_220, new __VLS_220({
    label: "备注",
}));
const __VLS_222 = __VLS_221({
    label: "备注",
}, ...__VLS_functionalComponentArgsRest(__VLS_221));
__VLS_223.slots.default;
const __VLS_224 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_225 = __VLS_asFunctionalComponent(__VLS_224, new __VLS_224({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
    maxlength: "500",
}));
const __VLS_226 = __VLS_225({
    modelValue: (__VLS_ctx.form.remark),
    type: "textarea",
    rows: (2),
    maxlength: "500",
}, ...__VLS_functionalComponentArgsRest(__VLS_225));
var __VLS_223;
if (__VLS_ctx.form.id) {
    const __VLS_228 = {}.ElFormItem;
    /** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_229 = __VLS_asFunctionalComponent(__VLS_228, new __VLS_228({
        label: "启用",
    }));
    const __VLS_230 = __VLS_229({
        label: "启用",
    }, ...__VLS_functionalComponentArgsRest(__VLS_229));
    __VLS_231.slots.default;
    const __VLS_232 = {}.ElSwitch;
    /** @type {[typeof __VLS_components.ElSwitch, typeof __VLS_components.elSwitch, ]} */ ;
    // @ts-ignore
    const __VLS_233 = __VLS_asFunctionalComponent(__VLS_232, new __VLS_232({
        modelValue: (__VLS_ctx.form.isActive),
    }));
    const __VLS_234 = __VLS_233({
        modelValue: (__VLS_ctx.form.isActive),
    }, ...__VLS_functionalComponentArgsRest(__VLS_233));
    var __VLS_231;
}
var __VLS_167;
{
    const { footer: __VLS_thisSlot } = __VLS_163.slots;
    const __VLS_236 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_237 = __VLS_asFunctionalComponent(__VLS_236, new __VLS_236({
        ...{ 'onClick': {} },
    }));
    const __VLS_238 = __VLS_237({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_237));
    let __VLS_240;
    let __VLS_241;
    let __VLS_242;
    const __VLS_243 = {
        onClick: (...[$event]) => {
            __VLS_ctx.formOpen = false;
        }
    };
    __VLS_239.slots.default;
    var __VLS_239;
    const __VLS_244 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_245 = __VLS_asFunctionalComponent(__VLS_244, new __VLS_244({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_246 = __VLS_245({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_245));
    let __VLS_248;
    let __VLS_249;
    let __VLS_250;
    const __VLS_251 = {
        onClick: (__VLS_ctx.save)
    };
    __VLS_247.slots.default;
    var __VLS_247;
}
var __VLS_163;
const __VLS_252 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_253 = __VLS_asFunctionalComponent(__VLS_252, new __VLS_252({
    modelValue: (__VLS_ctx.startOpen),
    title: (`开始计时：${__VLS_ctx.startTarget?.roomNo} 号房`),
    width: "400px",
}));
const __VLS_254 = __VLS_253({
    modelValue: (__VLS_ctx.startOpen),
    title: (`开始计时：${__VLS_ctx.startTarget?.roomNo} 号房`),
    width: "400px",
}, ...__VLS_functionalComponentArgsRest(__VLS_253));
__VLS_255.slots.default;
const __VLS_256 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_257 = __VLS_asFunctionalComponent(__VLS_256, new __VLS_256({
    labelWidth: "90px",
}));
const __VLS_258 = __VLS_257({
    labelWidth: "90px",
}, ...__VLS_functionalComponentArgsRest(__VLS_257));
__VLS_259.slots.default;
const __VLS_260 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_261 = __VLS_asFunctionalComponent(__VLS_260, new __VLS_260({
    label: "单价",
}));
const __VLS_262 = __VLS_261({
    label: "单价",
}, ...__VLS_functionalComponentArgsRest(__VLS_261));
__VLS_263.slots.default;
(__VLS_ctx.startTarget?.hourlyRate.toFixed(2));
var __VLS_263;
const __VLS_264 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_265 = __VLS_asFunctionalComponent(__VLS_264, new __VLS_264({
    label: "客户姓名",
}));
const __VLS_266 = __VLS_265({
    label: "客户姓名",
}, ...__VLS_functionalComponentArgsRest(__VLS_265));
__VLS_267.slots.default;
const __VLS_268 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_269 = __VLS_asFunctionalComponent(__VLS_268, new __VLS_268({
    modelValue: (__VLS_ctx.startForm.customerName),
    placeholder: "散客可填，会员另选",
    maxlength: "64",
}));
const __VLS_270 = __VLS_269({
    modelValue: (__VLS_ctx.startForm.customerName),
    placeholder: "散客可填，会员另选",
    maxlength: "64",
}, ...__VLS_functionalComponentArgsRest(__VLS_269));
var __VLS_267;
const __VLS_272 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_273 = __VLS_asFunctionalComponent(__VLS_272, new __VLS_272({
    label: "备注",
}));
const __VLS_274 = __VLS_273({
    label: "备注",
}, ...__VLS_functionalComponentArgsRest(__VLS_273));
__VLS_275.slots.default;
const __VLS_276 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_277 = __VLS_asFunctionalComponent(__VLS_276, new __VLS_276({
    modelValue: (__VLS_ctx.startForm.remark),
    maxlength: "200",
}));
const __VLS_278 = __VLS_277({
    modelValue: (__VLS_ctx.startForm.remark),
    maxlength: "200",
}, ...__VLS_functionalComponentArgsRest(__VLS_277));
var __VLS_275;
var __VLS_259;
{
    const { footer: __VLS_thisSlot } = __VLS_255.slots;
    const __VLS_280 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_281 = __VLS_asFunctionalComponent(__VLS_280, new __VLS_280({
        ...{ 'onClick': {} },
    }));
    const __VLS_282 = __VLS_281({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_281));
    let __VLS_284;
    let __VLS_285;
    let __VLS_286;
    const __VLS_287 = {
        onClick: (...[$event]) => {
            __VLS_ctx.startOpen = false;
        }
    };
    __VLS_283.slots.default;
    var __VLS_283;
    const __VLS_288 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_289 = __VLS_asFunctionalComponent(__VLS_288, new __VLS_288({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_290 = __VLS_289({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_289));
    let __VLS_292;
    let __VLS_293;
    let __VLS_294;
    const __VLS_295 = {
        onClick: (__VLS_ctx.doStart)
    };
    __VLS_291.slots.default;
    var __VLS_291;
}
var __VLS_255;
const __VLS_296 = {}.ElDialog;
/** @type {[typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, typeof __VLS_components.ElDialog, typeof __VLS_components.elDialog, ]} */ ;
// @ts-ignore
const __VLS_297 = __VLS_asFunctionalComponent(__VLS_296, new __VLS_296({
    modelValue: (__VLS_ctx.stopOpen),
    title: (`结束计时：${__VLS_ctx.stopTarget?.roomNo} 号房`),
    width: "400px",
}));
const __VLS_298 = __VLS_297({
    modelValue: (__VLS_ctx.stopOpen),
    title: (`结束计时：${__VLS_ctx.stopTarget?.roomNo} 号房`),
    width: "400px",
}, ...__VLS_functionalComponentArgsRest(__VLS_297));
__VLS_299.slots.default;
const __VLS_300 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_301 = __VLS_asFunctionalComponent(__VLS_300, new __VLS_300({
    labelWidth: "90px",
}));
const __VLS_302 = __VLS_301({
    labelWidth: "90px",
}, ...__VLS_functionalComponentArgsRest(__VLS_301));
__VLS_303.slots.default;
const __VLS_304 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_305 = __VLS_asFunctionalComponent(__VLS_304, new __VLS_304({
    label: "已计时",
}));
const __VLS_306 = __VLS_305({
    label: "已计时",
}, ...__VLS_functionalComponentArgsRest(__VLS_305));
__VLS_307.slots.default;
(__VLS_ctx.stopSession?.elapsedMinutes);
var __VLS_307;
const __VLS_308 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_309 = __VLS_asFunctionalComponent(__VLS_308, new __VLS_308({
    label: "预估金额",
}));
const __VLS_310 = __VLS_309({
    label: "预估金额",
}, ...__VLS_functionalComponentArgsRest(__VLS_309));
__VLS_311.slots.default;
(__VLS_ctx.estimatedAmount.toFixed(2));
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ class: "hint" },
});
var __VLS_311;
const __VLS_312 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_313 = __VLS_asFunctionalComponent(__VLS_312, new __VLS_312({
    label: "支付方式",
    required: true,
}));
const __VLS_314 = __VLS_313({
    label: "支付方式",
    required: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_313));
__VLS_315.slots.default;
const __VLS_316 = {}.ElRadioGroup;
/** @type {[typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, typeof __VLS_components.ElRadioGroup, typeof __VLS_components.elRadioGroup, ]} */ ;
// @ts-ignore
const __VLS_317 = __VLS_asFunctionalComponent(__VLS_316, new __VLS_316({
    modelValue: (__VLS_ctx.stopPayMethod),
}));
const __VLS_318 = __VLS_317({
    modelValue: (__VLS_ctx.stopPayMethod),
}, ...__VLS_functionalComponentArgsRest(__VLS_317));
__VLS_319.slots.default;
const __VLS_320 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_321 = __VLS_asFunctionalComponent(__VLS_320, new __VLS_320({
    value: "Cash",
}));
const __VLS_322 = __VLS_321({
    value: "Cash",
}, ...__VLS_functionalComponentArgsRest(__VLS_321));
__VLS_323.slots.default;
var __VLS_323;
const __VLS_324 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_325 = __VLS_asFunctionalComponent(__VLS_324, new __VLS_324({
    value: "Wechat",
}));
const __VLS_326 = __VLS_325({
    value: "Wechat",
}, ...__VLS_functionalComponentArgsRest(__VLS_325));
__VLS_327.slots.default;
var __VLS_327;
const __VLS_328 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_329 = __VLS_asFunctionalComponent(__VLS_328, new __VLS_328({
    value: "Alipay",
}));
const __VLS_330 = __VLS_329({
    value: "Alipay",
}, ...__VLS_functionalComponentArgsRest(__VLS_329));
__VLS_331.slots.default;
var __VLS_331;
const __VLS_332 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_333 = __VLS_asFunctionalComponent(__VLS_332, new __VLS_332({
    value: "MemberCard",
}));
const __VLS_334 = __VLS_333({
    value: "MemberCard",
}, ...__VLS_functionalComponentArgsRest(__VLS_333));
__VLS_335.slots.default;
var __VLS_335;
const __VLS_336 = {}.ElRadioButton;
/** @type {[typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, typeof __VLS_components.ElRadioButton, typeof __VLS_components.elRadioButton, ]} */ ;
// @ts-ignore
const __VLS_337 = __VLS_asFunctionalComponent(__VLS_336, new __VLS_336({
    value: "BankCard",
}));
const __VLS_338 = __VLS_337({
    value: "BankCard",
}, ...__VLS_functionalComponentArgsRest(__VLS_337));
__VLS_339.slots.default;
var __VLS_339;
var __VLS_319;
var __VLS_315;
var __VLS_303;
{
    const { footer: __VLS_thisSlot } = __VLS_299.slots;
    const __VLS_340 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_341 = __VLS_asFunctionalComponent(__VLS_340, new __VLS_340({
        ...{ 'onClick': {} },
    }));
    const __VLS_342 = __VLS_341({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_341));
    let __VLS_344;
    let __VLS_345;
    let __VLS_346;
    const __VLS_347 = {
        onClick: (...[$event]) => {
            __VLS_ctx.stopOpen = false;
        }
    };
    __VLS_343.slots.default;
    var __VLS_343;
    const __VLS_348 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_349 = __VLS_asFunctionalComponent(__VLS_348, new __VLS_348({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }));
    const __VLS_350 = __VLS_349({
        ...{ 'onClick': {} },
        type: "primary",
        loading: (__VLS_ctx.saving),
    }, ...__VLS_functionalComponentArgsRest(__VLS_349));
    let __VLS_352;
    let __VLS_353;
    let __VLS_354;
    const __VLS_355 = {
        onClick: (__VLS_ctx.doStop)
    };
    __VLS_351.slots.default;
    var __VLS_351;
}
var __VLS_299;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['title']} */ ;
/** @type {__VLS_StyleScopedClasses['spacer']} */ ;
/** @type {__VLS_StyleScopedClasses['hint']} */ ;
/** @type {__VLS_StyleScopedClasses['hint']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            Plus: Plus,
            Refresh: Refresh,
            rows: rows,
            loading: loading,
            includeInactive: includeInactive,
            formOpen: formOpen,
            saving: saving,
            form: form,
            timedSessions: timedSessions,
            timedOpen: timedOpen,
            timedStatusLabel: timedStatusLabel,
            payLabel: payLabel,
            canManage: canManage,
            activeStoreName: activeStoreName,
            reload: reload,
            openNew: openNew,
            openEdit: openEdit,
            save: save,
            startOpen: startOpen,
            startTarget: startTarget,
            startForm: startForm,
            openStart: openStart,
            doStart: doStart,
            stopOpen: stopOpen,
            stopTarget: stopTarget,
            stopSession: stopSession,
            stopPayMethod: stopPayMethod,
            estimatedAmount: estimatedAmount,
            openStop: openStop,
            doStop: doStop,
            cancelSession: cancelSession,
            remove: remove,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
