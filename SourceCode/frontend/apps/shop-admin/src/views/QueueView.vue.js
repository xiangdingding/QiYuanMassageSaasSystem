import { computed, onMounted, onUnmounted, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Bell, Refresh } from '@element-plus/icons-vue';
import dayjs from 'dayjs';
import { queueApi, staffApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import { useAuthStore } from '@/stores/auth';
const appStore = useAppStore();
const auth = useAuthStore();
const rows = ref([]);
const loading = ref(false);
const lastCalled = ref(null);
const canManage = computed(() => auth.role !== 'Technician');
const counts = computed(() => {
    const c = { OnDuty: 0, Resting: 0, OffDuty: 0, Idle: 0 };
    rows.value.forEach((r) => { c[r.state] = (c[r.state] || 0) + 1; });
    return c;
});
function stateLabel(s) {
    return { OnDuty: '在岗', Resting: '休息', OffDuty: '下班', Idle: '空闲' }[s] ?? s;
}
function stateType(s) {
    return { OnDuty: 'success', Resting: 'warning', OffDuty: 'info', Idle: '' }[s] ?? '';
}
async function reload() {
    if (!appStore.activeStoreId)
        return;
    loading.value = true;
    try {
        const data = await queueApi.list(appStore.activeStoreId);
        // 若后端返回的人少于已知技师人数，前端补一行 OffDuty 占位
        const staff = await staffApi.list({ role: 'Technician', storeId: appStore.activeStoreId, pageSize: 200 });
        const placeholders = staff.items
            .filter((t) => !data.find((q) => q.technicianId === t.id))
            .map((t) => ({
            id: 0,
            technicianId: t.id,
            technicianName: t.realName ?? t.username,
            employeeNo: t.employeeNo ?? null,
            state: 'OffDuty',
            queuePosition: 0,
            todayRoundCount: 0,
            enteredAt: null,
            lastCalledAt: null
        }));
        rows.value = [...data, ...placeholders];
    }
    finally {
        loading.value = false;
    }
}
async function setState(technicianId, state) {
    await queueApi.setState(technicianId, state);
    await reload();
}
async function callNext() {
    if (!appStore.activeStoreId)
        return;
    const result = await queueApi.callNext(appStore.activeStoreId);
    if (!result.technicianId) {
        ElMessage.warning('没有在岗的技师');
        lastCalled.value = null;
    }
    else {
        lastCalled.value = {
            technicianName: result.technicianName ?? '',
            employeeNo: result.employeeNo ?? null
        };
        ElMessage.success(`已叫 ${result.employeeNo ?? ''} 号 · ${result.technicianName ?? ''}`);
        await reload();
    }
}
async function resetDay() {
    if (!appStore.activeStoreId)
        return;
    await ElMessageBox.confirm('确认重置今日所有技师轮次？', '提示', { type: 'warning' }).catch(() => null);
    await queueApi.resetDay(appStore.activeStoreId);
    ElMessage.success('已重置');
    reload();
}
let timer = null;
onMounted(async () => {
    await appStore.loadStores();
    await reload();
    timer = window.setInterval(reload, 15000);
});
onUnmounted(() => {
    if (timer != null)
        window.clearInterval(timer);
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
const __VLS_4 = {}.ElTag;
/** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
// @ts-ignore
const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
    size: "large",
}));
const __VLS_6 = __VLS_5({
    size: "large",
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
__VLS_7.slots.default;
(__VLS_ctx.counts.OnDuty);
var __VLS_7;
const __VLS_8 = {}.ElTag;
/** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
// @ts-ignore
const __VLS_9 = __VLS_asFunctionalComponent(__VLS_8, new __VLS_8({
    size: "large",
    type: "warning",
}));
const __VLS_10 = __VLS_9({
    size: "large",
    type: "warning",
}, ...__VLS_functionalComponentArgsRest(__VLS_9));
__VLS_11.slots.default;
(__VLS_ctx.counts.Resting);
var __VLS_11;
const __VLS_12 = {}.ElTag;
/** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
    size: "large",
    type: "info",
}));
const __VLS_14 = __VLS_13({
    size: "large",
    type: "info",
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
__VLS_15.slots.default;
(__VLS_ctx.counts.OffDuty);
var __VLS_15;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div)({
    ...{ class: "spacer" },
});
if (__VLS_ctx.canManage) {
    const __VLS_16 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({
        ...{ 'onClick': {} },
        type: "primary",
        icon: (__VLS_ctx.Bell),
    }));
    const __VLS_18 = __VLS_17({
        ...{ 'onClick': {} },
        type: "primary",
        icon: (__VLS_ctx.Bell),
    }, ...__VLS_functionalComponentArgsRest(__VLS_17));
    let __VLS_20;
    let __VLS_21;
    let __VLS_22;
    const __VLS_23 = {
        onClick: (__VLS_ctx.callNext)
    };
    __VLS_19.slots.default;
    var __VLS_19;
}
if (__VLS_ctx.canManage) {
    const __VLS_24 = {}.ElButton;
    /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
    // @ts-ignore
    const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
        ...{ 'onClick': {} },
        icon: (__VLS_ctx.Refresh),
    }));
    const __VLS_26 = __VLS_25({
        ...{ 'onClick': {} },
        icon: (__VLS_ctx.Refresh),
    }, ...__VLS_functionalComponentArgsRest(__VLS_25));
    let __VLS_28;
    let __VLS_29;
    let __VLS_30;
    const __VLS_31 = {
        onClick: (__VLS_ctx.resetDay)
    };
    __VLS_27.slots.default;
    var __VLS_27;
}
if (__VLS_ctx.lastCalled) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "called" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.strong, __VLS_intrinsicElements.strong)({});
    (__VLS_ctx.lastCalled.employeeNo ?? '-');
    (__VLS_ctx.lastCalled.technicianName);
}
const __VLS_32 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_34 = __VLS_33({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_33));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_35.slots.default;
const __VLS_36 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
    prop: "employeeNo",
    label: "工号",
    width: "80",
}));
const __VLS_38 = __VLS_37({
    prop: "employeeNo",
    label: "工号",
    width: "80",
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
const __VLS_40 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
    prop: "technicianName",
    label: "姓名",
    minWidth: "120",
}));
const __VLS_42 = __VLS_41({
    prop: "technicianName",
    label: "姓名",
    minWidth: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_41));
const __VLS_44 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    label: "状态",
    width: "120",
}));
const __VLS_46 = __VLS_45({
    label: "状态",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
__VLS_47.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_47.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_48 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
        type: (__VLS_ctx.stateType(row.state)),
    }));
    const __VLS_50 = __VLS_49({
        type: (__VLS_ctx.stateType(row.state)),
    }, ...__VLS_functionalComponentArgsRest(__VLS_49));
    __VLS_51.slots.default;
    (__VLS_ctx.stateLabel(row.state));
    var __VLS_51;
}
var __VLS_47;
const __VLS_52 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
    prop: "queuePosition",
    label: "排队号",
    width: "80",
}));
const __VLS_54 = __VLS_53({
    prop: "queuePosition",
    label: "排队号",
    width: "80",
}, ...__VLS_functionalComponentArgsRest(__VLS_53));
const __VLS_56 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
    prop: "todayRoundCount",
    label: "今日轮次",
    width: "100",
}));
const __VLS_58 = __VLS_57({
    prop: "todayRoundCount",
    label: "今日轮次",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_57));
const __VLS_60 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
    label: "进入时间",
    width: "160",
}));
const __VLS_62 = __VLS_61({
    label: "进入时间",
    width: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_61));
__VLS_63.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_63.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.enteredAt ? __VLS_ctx.dayjs(row.enteredAt).format('HH:mm') : '—');
}
var __VLS_63;
const __VLS_64 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
    label: "最近叫号",
    width: "160",
}));
const __VLS_66 = __VLS_65({
    label: "最近叫号",
    width: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_65));
__VLS_67.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_67.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (row.lastCalledAt ? __VLS_ctx.dayjs(row.lastCalledAt).format('HH:mm') : '—');
}
var __VLS_67;
if (__VLS_ctx.canManage) {
    const __VLS_68 = {}.ElTableColumn;
    /** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
    // @ts-ignore
    const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
        label: "操作",
        width: "320",
        fixed: "right",
    }));
    const __VLS_70 = __VLS_69({
        label: "操作",
        width: "320",
        fixed: "right",
    }, ...__VLS_functionalComponentArgsRest(__VLS_69));
    __VLS_71.slots.default;
    {
        const { default: __VLS_thisSlot } = __VLS_71.slots;
        const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
        const __VLS_72 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
            ...{ 'onClick': {} },
            size: "small",
            type: (row.state === 'OnDuty' ? '' : 'primary'),
            disabled: (row.state === 'OnDuty'),
        }));
        const __VLS_74 = __VLS_73({
            ...{ 'onClick': {} },
            size: "small",
            type: (row.state === 'OnDuty' ? '' : 'primary'),
            disabled: (row.state === 'OnDuty'),
        }, ...__VLS_functionalComponentArgsRest(__VLS_73));
        let __VLS_76;
        let __VLS_77;
        let __VLS_78;
        const __VLS_79 = {
            onClick: (...[$event]) => {
                if (!(__VLS_ctx.canManage))
                    return;
                __VLS_ctx.setState(row.technicianId, 'OnDuty');
            }
        };
        __VLS_75.slots.default;
        var __VLS_75;
        const __VLS_80 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
            ...{ 'onClick': {} },
            size: "small",
            type: (row.state === 'Resting' ? '' : 'warning'),
            disabled: (row.state === 'Resting' || row.state === 'OffDuty'),
        }));
        const __VLS_82 = __VLS_81({
            ...{ 'onClick': {} },
            size: "small",
            type: (row.state === 'Resting' ? '' : 'warning'),
            disabled: (row.state === 'Resting' || row.state === 'OffDuty'),
        }, ...__VLS_functionalComponentArgsRest(__VLS_81));
        let __VLS_84;
        let __VLS_85;
        let __VLS_86;
        const __VLS_87 = {
            onClick: (...[$event]) => {
                if (!(__VLS_ctx.canManage))
                    return;
                __VLS_ctx.setState(row.technicianId, 'Resting');
            }
        };
        __VLS_83.slots.default;
        var __VLS_83;
        const __VLS_88 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
            ...{ 'onClick': {} },
            size: "small",
            type: (row.state === 'OffDuty' ? '' : 'info'),
            disabled: (row.state === 'OffDuty'),
        }));
        const __VLS_90 = __VLS_89({
            ...{ 'onClick': {} },
            size: "small",
            type: (row.state === 'OffDuty' ? '' : 'info'),
            disabled: (row.state === 'OffDuty'),
        }, ...__VLS_functionalComponentArgsRest(__VLS_89));
        let __VLS_92;
        let __VLS_93;
        let __VLS_94;
        const __VLS_95 = {
            onClick: (...[$event]) => {
                if (!(__VLS_ctx.canManage))
                    return;
                __VLS_ctx.setState(row.technicianId, 'OffDuty');
            }
        };
        __VLS_91.slots.default;
        var __VLS_91;
    }
    var __VLS_71;
}
var __VLS_35;
var __VLS_3;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['title']} */ ;
/** @type {__VLS_StyleScopedClasses['spacer']} */ ;
/** @type {__VLS_StyleScopedClasses['called']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            Bell: Bell,
            Refresh: Refresh,
            dayjs: dayjs,
            rows: rows,
            loading: loading,
            lastCalled: lastCalled,
            canManage: canManage,
            counts: counts,
            stateLabel: stateLabel,
            stateType: stateType,
            setState: setState,
            callNext: callNext,
            resetDay: resetDay,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
