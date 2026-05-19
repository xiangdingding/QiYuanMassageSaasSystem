import { onMounted, reactive, ref, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import dayjs from 'dayjs';
import { appointmentsApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
const appStore = useAppStore();
const query = reactive({
    page: 1, pageSize: 20, status: ''
});
const dateRange = ref(null);
const rows = ref([]);
const total = ref(0);
const loading = ref(false);
const STATUS_LABELS = {
    Pending: '待确认', Confirmed: '已确认', Arrived: '已到店',
    Completed: '已完成', Cancelled: '已取消', NoShow: '未到店'
};
const STATUS_TYPES = {
    Pending: 'warning', Confirmed: 'primary', Arrived: 'success',
    Completed: '', Cancelled: 'danger', NoShow: 'info'
};
function statusLabel(s) { return STATUS_LABELS[s] ?? s; }
function statusType(s) { return STATUS_TYPES[s] ?? ''; }
async function reload() {
    if (!appStore.activeStoreId)
        return;
    loading.value = true;
    try {
        const r = await appointmentsApi.list({
            storeId: appStore.activeStoreId,
            status: query.status || undefined,
            from: dateRange.value?.[0],
            to: dateRange.value?.[1] ? dayjs(dateRange.value[1]).add(1, 'day').format('YYYY-MM-DD') : undefined,
            page: query.page,
            pageSize: query.pageSize
        });
        rows.value = r.items;
        total.value = r.total;
    }
    finally {
        loading.value = false;
    }
}
function resetQuery() {
    query.status = '';
    dateRange.value = null;
    query.page = 1;
    reload();
}
async function confirm(row) {
    await appointmentsApi.confirm(row.id);
    ElMessage.success('已确认');
    await reload();
}
async function arrive(row) {
    await appointmentsApi.arrive(row.id);
    ElMessage.success('已到店');
    await reload();
}
async function cancel(row) {
    const { value } = await ElMessageBox.prompt('请输入取消原因（可选）', '取消预约', {
        inputPlaceholder: '比如：客人临时改期'
    }).catch(() => ({ value: null }));
    if (value === null)
        return;
    await appointmentsApi.cancel(row.id, value || null);
    ElMessage.success('已取消');
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
const __VLS_4 = {}.ElSelect;
/** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
// @ts-ignore
const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
    modelValue: (__VLS_ctx.query.status),
    placeholder: "全部状态",
    clearable: true,
    ...{ style: {} },
}));
const __VLS_6 = __VLS_5({
    modelValue: (__VLS_ctx.query.status),
    placeholder: "全部状态",
    clearable: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
__VLS_7.slots.default;
const __VLS_8 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_9 = __VLS_asFunctionalComponent(__VLS_8, new __VLS_8({
    label: "待确认",
    value: "Pending",
}));
const __VLS_10 = __VLS_9({
    label: "待确认",
    value: "Pending",
}, ...__VLS_functionalComponentArgsRest(__VLS_9));
const __VLS_12 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
    label: "已确认",
    value: "Confirmed",
}));
const __VLS_14 = __VLS_13({
    label: "已确认",
    value: "Confirmed",
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
const __VLS_16 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({
    label: "已到店",
    value: "Arrived",
}));
const __VLS_18 = __VLS_17({
    label: "已到店",
    value: "Arrived",
}, ...__VLS_functionalComponentArgsRest(__VLS_17));
const __VLS_20 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
    label: "已完成",
    value: "Completed",
}));
const __VLS_22 = __VLS_21({
    label: "已完成",
    value: "Completed",
}, ...__VLS_functionalComponentArgsRest(__VLS_21));
const __VLS_24 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
    label: "已取消",
    value: "Cancelled",
}));
const __VLS_26 = __VLS_25({
    label: "已取消",
    value: "Cancelled",
}, ...__VLS_functionalComponentArgsRest(__VLS_25));
const __VLS_28 = {}.ElOption;
/** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
    label: "未到店",
    value: "NoShow",
}));
const __VLS_30 = __VLS_29({
    label: "未到店",
    value: "NoShow",
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
var __VLS_7;
const __VLS_32 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
    modelValue: (__VLS_ctx.dateRange),
    type: "daterange",
    rangeSeparator: "至",
    startPlaceholder: "开始日期",
    endPlaceholder: "结束日期",
    format: "YYYY-MM-DD",
    valueFormat: "YYYY-MM-DD",
}));
const __VLS_34 = __VLS_33({
    modelValue: (__VLS_ctx.dateRange),
    type: "daterange",
    rangeSeparator: "至",
    startPlaceholder: "开始日期",
    endPlaceholder: "结束日期",
    format: "YYYY-MM-DD",
    valueFormat: "YYYY-MM-DD",
}, ...__VLS_functionalComponentArgsRest(__VLS_33));
const __VLS_36 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
    ...{ 'onClick': {} },
    type: "primary",
}));
const __VLS_38 = __VLS_37({
    ...{ 'onClick': {} },
    type: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
let __VLS_40;
let __VLS_41;
let __VLS_42;
const __VLS_43 = {
    onClick: (__VLS_ctx.reload)
};
__VLS_39.slots.default;
var __VLS_39;
const __VLS_44 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    ...{ 'onClick': {} },
}));
const __VLS_46 = __VLS_45({
    ...{ 'onClick': {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
let __VLS_48;
let __VLS_49;
let __VLS_50;
const __VLS_51 = {
    onClick: (__VLS_ctx.resetQuery)
};
__VLS_47.slots.default;
var __VLS_47;
const __VLS_52 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}));
const __VLS_54 = __VLS_53({
    data: (__VLS_ctx.rows),
    stripe: true,
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_53));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_55.slots.default;
const __VLS_56 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
    label: "到店时间",
    width: "170",
}));
const __VLS_58 = __VLS_57({
    label: "到店时间",
    width: "170",
}, ...__VLS_functionalComponentArgsRest(__VLS_57));
__VLS_59.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_59.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    (__VLS_ctx.dayjs(row.expectedArriveAt).format('YYYY-MM-DD HH:mm'));
}
var __VLS_59;
const __VLS_60 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
    prop: "customerName",
    label: "姓名",
    width: "120",
}));
const __VLS_62 = __VLS_61({
    prop: "customerName",
    label: "姓名",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_61));
const __VLS_64 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
    prop: "customerPhone",
    label: "电话",
    width: "130",
}));
const __VLS_66 = __VLS_65({
    prop: "customerPhone",
    label: "电话",
    width: "130",
}, ...__VLS_functionalComponentArgsRest(__VLS_65));
const __VLS_68 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
    label: "人数",
    width: "60",
    prop: "partySize",
}));
const __VLS_70 = __VLS_69({
    label: "人数",
    width: "60",
    prop: "partySize",
}, ...__VLS_functionalComponentArgsRest(__VLS_69));
const __VLS_72 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
    label: "服务",
    minWidth: "120",
    prop: "serviceName",
}));
const __VLS_74 = __VLS_73({
    label: "服务",
    minWidth: "120",
    prop: "serviceName",
}, ...__VLS_functionalComponentArgsRest(__VLS_73));
const __VLS_76 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
    label: "指定技师",
    width: "140",
    prop: "preferredTechnicianName",
}));
const __VLS_78 = __VLS_77({
    label: "指定技师",
    width: "140",
    prop: "preferredTechnicianName",
}, ...__VLS_functionalComponentArgsRest(__VLS_77));
const __VLS_80 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
    label: "状态",
    width: "100",
}));
const __VLS_82 = __VLS_81({
    label: "状态",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_81));
__VLS_83.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_83.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    const __VLS_84 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_85 = __VLS_asFunctionalComponent(__VLS_84, new __VLS_84({
        type: (__VLS_ctx.statusType(row.status)),
    }));
    const __VLS_86 = __VLS_85({
        type: (__VLS_ctx.statusType(row.status)),
    }, ...__VLS_functionalComponentArgsRest(__VLS_85));
    __VLS_87.slots.default;
    (__VLS_ctx.statusLabel(row.status));
    var __VLS_87;
}
var __VLS_83;
const __VLS_88 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
    label: "备注",
    minWidth: "160",
    prop: "remark",
    showOverflowTooltip: true,
}));
const __VLS_90 = __VLS_89({
    label: "备注",
    minWidth: "160",
    prop: "remark",
    showOverflowTooltip: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_89));
const __VLS_92 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_93 = __VLS_asFunctionalComponent(__VLS_92, new __VLS_92({
    label: "操作",
    width: "220",
    fixed: "right",
}));
const __VLS_94 = __VLS_93({
    label: "操作",
    width: "220",
    fixed: "right",
}, ...__VLS_functionalComponentArgsRest(__VLS_93));
__VLS_95.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_95.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    if (row.status === 'Pending') {
        const __VLS_96 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({
            ...{ 'onClick': {} },
            size: "small",
            type: "primary",
            'aria-label': (`确认 ${row.customerName} 的预约`),
        }));
        const __VLS_98 = __VLS_97({
            ...{ 'onClick': {} },
            size: "small",
            type: "primary",
            'aria-label': (`确认 ${row.customerName} 的预约`),
        }, ...__VLS_functionalComponentArgsRest(__VLS_97));
        let __VLS_100;
        let __VLS_101;
        let __VLS_102;
        const __VLS_103 = {
            onClick: (...[$event]) => {
                if (!(row.status === 'Pending'))
                    return;
                __VLS_ctx.confirm(row);
            }
        };
        __VLS_99.slots.default;
        var __VLS_99;
    }
    if (row.status === 'Pending' || row.status === 'Confirmed') {
        const __VLS_104 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_105 = __VLS_asFunctionalComponent(__VLS_104, new __VLS_104({
            ...{ 'onClick': {} },
            size: "small",
            type: "success",
            'aria-label': (`标记 ${row.customerName} 已到店`),
        }));
        const __VLS_106 = __VLS_105({
            ...{ 'onClick': {} },
            size: "small",
            type: "success",
            'aria-label': (`标记 ${row.customerName} 已到店`),
        }, ...__VLS_functionalComponentArgsRest(__VLS_105));
        let __VLS_108;
        let __VLS_109;
        let __VLS_110;
        const __VLS_111 = {
            onClick: (...[$event]) => {
                if (!(row.status === 'Pending' || row.status === 'Confirmed'))
                    return;
                __VLS_ctx.arrive(row);
            }
        };
        __VLS_107.slots.default;
        var __VLS_107;
    }
    if (row.status === 'Pending' || row.status === 'Confirmed') {
        const __VLS_112 = {}.ElButton;
        /** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
        // @ts-ignore
        const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({
            ...{ 'onClick': {} },
            size: "small",
            type: "danger",
            'aria-label': (`取消 ${row.customerName} 的预约`),
        }));
        const __VLS_114 = __VLS_113({
            ...{ 'onClick': {} },
            size: "small",
            type: "danger",
            'aria-label': (`取消 ${row.customerName} 的预约`),
        }, ...__VLS_functionalComponentArgsRest(__VLS_113));
        let __VLS_116;
        let __VLS_117;
        let __VLS_118;
        const __VLS_119 = {
            onClick: (...[$event]) => {
                if (!(row.status === 'Pending' || row.status === 'Confirmed'))
                    return;
                __VLS_ctx.cancel(row);
            }
        };
        __VLS_115.slots.default;
        var __VLS_115;
    }
}
var __VLS_95;
var __VLS_55;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "pager" },
});
const __VLS_120 = {}.ElPagination;
/** @type {[typeof __VLS_components.ElPagination, typeof __VLS_components.elPagination, ]} */ ;
// @ts-ignore
const __VLS_121 = __VLS_asFunctionalComponent(__VLS_120, new __VLS_120({
    ...{ 'onChange': {} },
    currentPage: (__VLS_ctx.query.page),
    pageSize: (__VLS_ctx.query.pageSize),
    total: (__VLS_ctx.total),
    layout: "total, sizes, prev, pager, next, jumper",
    pageSizes: ([20, 50, 100]),
}));
const __VLS_122 = __VLS_121({
    ...{ 'onChange': {} },
    currentPage: (__VLS_ctx.query.page),
    pageSize: (__VLS_ctx.query.pageSize),
    total: (__VLS_ctx.total),
    layout: "total, sizes, prev, pager, next, jumper",
    pageSizes: ([20, 50, 100]),
}, ...__VLS_functionalComponentArgsRest(__VLS_121));
let __VLS_124;
let __VLS_125;
let __VLS_126;
const __VLS_127 = {
    onChange: (__VLS_ctx.reload)
};
var __VLS_123;
var __VLS_3;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['pager']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            dayjs: dayjs,
            query: query,
            dateRange: dateRange,
            rows: rows,
            total: total,
            loading: loading,
            statusLabel: statusLabel,
            statusType: statusType,
            reload: reload,
            resetQuery: resetQuery,
            confirm: confirm,
            arrive: arrive,
            cancel: cancel,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
