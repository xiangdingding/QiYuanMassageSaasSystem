import { onMounted, ref, watch } from 'vue';
import { Refresh } from '@element-plus/icons-vue';
import { reviewsApi } from '@/api/modules';
const tab = ref('list');
const rows = ref([]);
const summary = ref([]);
const loading = ref(false);
const ratingFilter = ref(undefined);
const dateRange = ref(null);
async function reload() {
    loading.value = true;
    try {
        const from = dateRange.value?.[0]?.toISOString();
        const to = dateRange.value?.[1]?.toISOString();
        if (tab.value === 'list') {
            rows.value = await reviewsApi.list({
                rating: ratingFilter.value,
                from, to
            });
        }
        else {
            const data = await reviewsApi.technicianSummary({ from, to });
            summary.value = data;
        }
    }
    finally {
        loading.value = false;
    }
}
function onTabChange() { reload(); }
watch([ratingFilter, dateRange], () => reload());
onMounted(reload);
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
const __VLS_4 = {}.ElInputNumber;
/** @type {[typeof __VLS_components.ElInputNumber, typeof __VLS_components.elInputNumber, ]} */ ;
// @ts-ignore
const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
    modelValue: (__VLS_ctx.ratingFilter),
    min: (1),
    max: (5),
    controlsPosition: "right",
    placeholder: "星级",
    ...{ style: {} },
}));
const __VLS_6 = __VLS_5({
    modelValue: (__VLS_ctx.ratingFilter),
    min: (1),
    max: (5),
    controlsPosition: "right",
    placeholder: "星级",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
const __VLS_8 = {}.ElDatePicker;
/** @type {[typeof __VLS_components.ElDatePicker, typeof __VLS_components.elDatePicker, ]} */ ;
// @ts-ignore
const __VLS_9 = __VLS_asFunctionalComponent(__VLS_8, new __VLS_8({
    modelValue: (__VLS_ctx.dateRange),
    type: "daterange",
    rangeSeparator: "-",
    startPlaceholder: "开始",
    endPlaceholder: "结束",
}));
const __VLS_10 = __VLS_9({
    modelValue: (__VLS_ctx.dateRange),
    type: "daterange",
    rangeSeparator: "-",
    startPlaceholder: "开始",
    endPlaceholder: "结束",
}, ...__VLS_functionalComponentArgsRest(__VLS_9));
__VLS_asFunctionalElement(__VLS_intrinsicElements.div)({
    ...{ class: "spacer" },
});
const __VLS_12 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}));
const __VLS_14 = __VLS_13({
    ...{ 'onClick': {} },
    icon: (__VLS_ctx.Refresh),
}, ...__VLS_functionalComponentArgsRest(__VLS_13));
let __VLS_16;
let __VLS_17;
let __VLS_18;
const __VLS_19 = {
    onClick: (__VLS_ctx.reload)
};
__VLS_15.slots.default;
var __VLS_15;
const __VLS_20 = {}.ElTabs;
/** @type {[typeof __VLS_components.ElTabs, typeof __VLS_components.elTabs, typeof __VLS_components.ElTabs, typeof __VLS_components.elTabs, ]} */ ;
// @ts-ignore
const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
    ...{ 'onTabChange': {} },
    modelValue: (__VLS_ctx.tab),
    ...{ style: {} },
}));
const __VLS_22 = __VLS_21({
    ...{ 'onTabChange': {} },
    modelValue: (__VLS_ctx.tab),
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_21));
let __VLS_24;
let __VLS_25;
let __VLS_26;
const __VLS_27 = {
    onTabChange: (__VLS_ctx.onTabChange)
};
__VLS_23.slots.default;
const __VLS_28 = {}.ElTabPane;
/** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
    label: "全部评价",
    name: "list",
}));
const __VLS_30 = __VLS_29({
    label: "全部评价",
    name: "list",
}, ...__VLS_functionalComponentArgsRest(__VLS_29));
__VLS_31.slots.default;
const __VLS_32 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
    data: (__VLS_ctx.rows),
    stripe: true,
}));
const __VLS_34 = __VLS_33({
    data: (__VLS_ctx.rows),
    stripe: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_33));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_35.slots.default;
const __VLS_36 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
    prop: "technicianName",
    label: "技师",
    width: "120",
}));
const __VLS_38 = __VLS_37({
    prop: "technicianName",
    label: "技师",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_37));
const __VLS_40 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
    prop: "memberName",
    label: "顾客",
    width: "120",
}));
const __VLS_42 = __VLS_41({
    prop: "memberName",
    label: "顾客",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_41));
const __VLS_44 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    prop: "rating",
    label: "评分",
    width: "100",
}));
const __VLS_46 = __VLS_45({
    prop: "rating",
    label: "评分",
    width: "100",
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
__VLS_47.slots.default;
{
    const { default: __VLS_thisSlot } = __VLS_47.slots;
    const [{ row }] = __VLS_getSlotParams(__VLS_thisSlot);
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
        'aria-label': (`${row.rating} 星`),
    });
    ('★'.repeat(row.rating));
}
var __VLS_47;
const __VLS_48 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
    prop: "tags",
    label: "标签",
    width: "200",
}));
const __VLS_50 = __VLS_49({
    prop: "tags",
    label: "标签",
    width: "200",
}, ...__VLS_functionalComponentArgsRest(__VLS_49));
const __VLS_52 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
    prop: "comment",
    label: "评论",
    minWidth: "240",
    showOverflowTooltip: true,
}));
const __VLS_54 = __VLS_53({
    prop: "comment",
    label: "评论",
    minWidth: "240",
    showOverflowTooltip: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_53));
const __VLS_56 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
    prop: "createdAt",
    label: "时间",
    width: "180",
}));
const __VLS_58 = __VLS_57({
    prop: "createdAt",
    label: "时间",
    width: "180",
}, ...__VLS_functionalComponentArgsRest(__VLS_57));
var __VLS_35;
var __VLS_31;
const __VLS_60 = {}.ElTabPane;
/** @type {[typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, typeof __VLS_components.ElTabPane, typeof __VLS_components.elTabPane, ]} */ ;
// @ts-ignore
const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
    label: "技师汇总",
    name: "summary",
}));
const __VLS_62 = __VLS_61({
    label: "技师汇总",
    name: "summary",
}, ...__VLS_functionalComponentArgsRest(__VLS_61));
__VLS_63.slots.default;
const __VLS_64 = {}.ElTable;
/** @type {[typeof __VLS_components.ElTable, typeof __VLS_components.elTable, typeof __VLS_components.ElTable, typeof __VLS_components.elTable, ]} */ ;
// @ts-ignore
const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
    data: (__VLS_ctx.summary),
    stripe: true,
}));
const __VLS_66 = __VLS_65({
    data: (__VLS_ctx.summary),
    stripe: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_65));
__VLS_asFunctionalDirective(__VLS_directives.vLoading)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.loading) }, null, null);
__VLS_67.slots.default;
const __VLS_68 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
    prop: "technicianName",
    label: "技师",
    width: "160",
}));
const __VLS_70 = __VLS_69({
    prop: "technicianName",
    label: "技师",
    width: "160",
}, ...__VLS_functionalComponentArgsRest(__VLS_69));
const __VLS_72 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
    prop: "reviewCount",
    label: "评价数",
    width: "120",
}));
const __VLS_74 = __VLS_73({
    prop: "reviewCount",
    label: "评价数",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_73));
const __VLS_76 = {}.ElTableColumn;
/** @type {[typeof __VLS_components.ElTableColumn, typeof __VLS_components.elTableColumn, ]} */ ;
// @ts-ignore
const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
    prop: "averageRating",
    label: "平均分",
    width: "120",
}));
const __VLS_78 = __VLS_77({
    prop: "averageRating",
    label: "平均分",
    width: "120",
}, ...__VLS_functionalComponentArgsRest(__VLS_77));
var __VLS_67;
var __VLS_63;
var __VLS_23;
var __VLS_3;
/** @type {__VLS_StyleScopedClasses['page']} */ ;
/** @type {__VLS_StyleScopedClasses['toolbar']} */ ;
/** @type {__VLS_StyleScopedClasses['title']} */ ;
/** @type {__VLS_StyleScopedClasses['spacer']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            Refresh: Refresh,
            tab: tab,
            rows: rows,
            summary: summary,
            loading: loading,
            ratingFilter: ratingFilter,
            dateRange: dateRange,
            reload: reload,
            onTabChange: onTabChange,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
