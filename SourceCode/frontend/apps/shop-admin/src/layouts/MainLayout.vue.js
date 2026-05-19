import { computed, onMounted, reactive } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { AlarmClock, Avatar, Box, Calendar, CreditCard, Discount, Goods, House, List, Money, OfficeBuilding, StarFilled, TakeawayBox, Tickets, TrendCharts, User, UserFilled, Wallet, WarnTriangleFilled } from '@element-plus/icons-vue';
import { canSee, useAuthStore } from '@/stores/auth';
import { useAppStore } from '@/stores/app';
import { subscriptionsApi } from '@/api/modules';
const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const appStore = useAppStore();
const subStore = reactive({
    daysToExpire: null,
    expired: false,
    status: null
});
const ROLE_LABELS = {
    PlatformAdmin: '平台管理员',
    ShopOwner: '店主',
    StoreManager: '店长',
    Cashier: '收银员',
    Technician: '技师'
};
const roleLabel = computed(() => (auth.user?.role ? ROLE_LABELS[auth.user.role] : ''));
const ICONS = {
    AlarmClock, Avatar, Box, Calendar, CreditCard, Discount, Goods, House,
    List, Money, OfficeBuilding, StarFilled, TakeawayBox, Tickets, TrendCharts,
    User, UserFilled, Wallet, WarnTriangleFilled
};
function iconCmp(name) {
    return ICONS[name] ?? Tickets;
}
const visibleMenu = computed(() => {
    const layout = router.options.routes.find((r) => r.path === '/');
    if (!layout?.children)
        return [];
    return layout.children
        .filter((c) => c.meta?.menu)
        .map((c) => ({
        path: '/' + (c.path || ''),
        title: c.meta?.title,
        icon: c.meta?.icon,
        roles: c.meta?.roles
    }))
        .filter((m) => canSee(m.roles, auth.role));
});
const pageTitle = computed(() => route.meta.title ?? '');
const activeStoreName = computed(() => appStore.stores.find((s) => s.id === appStore.activeStoreId)?.name ?? '');
const expireWarn = computed(() => subStore.daysToExpire !== null && subStore.daysToExpire > 0 && subStore.daysToExpire <= 30 && !subStore.expired);
function onCommand(cmd) {
    if (cmd === 'logout') {
        auth.logout();
        router.replace('/login');
    }
}
onMounted(async () => {
    await appStore.loadStores().catch(() => null);
    if (auth.user?.role === 'ShopOwner' || auth.user?.role === 'StoreManager') {
        try {
            const sub = await subscriptionsApi.me();
            subStore.daysToExpire = sub.daysToExpire ?? null;
            subStore.status = sub.status;
            subStore.expired = sub.status !== 'Active';
        }
        catch {
            /* ignore */
        }
    }
});
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
/** @type {__VLS_StyleScopedClasses['skip-link']} */ ;
// CSS variable injection 
// CSS variable injection end 
const __VLS_0 = {}.ElContainer;
/** @type {[typeof __VLS_components.ElContainer, typeof __VLS_components.elContainer, typeof __VLS_components.ElContainer, typeof __VLS_components.elContainer, ]} */ ;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({
    ...{ class: "layout" },
}));
const __VLS_2 = __VLS_1({
    ...{ class: "layout" },
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
var __VLS_4 = {};
__VLS_3.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.a, __VLS_intrinsicElements.a)({
    href: "#main-content",
    ...{ class: "skip-link" },
    'aria-label': "跳到主要内容",
});
const __VLS_5 = {}.ElAside;
/** @type {[typeof __VLS_components.ElAside, typeof __VLS_components.elAside, typeof __VLS_components.ElAside, typeof __VLS_components.elAside, ]} */ ;
// @ts-ignore
const __VLS_6 = __VLS_asFunctionalComponent(__VLS_5, new __VLS_5({
    width: "220px",
    ...{ class: "aside" },
    role: "navigation",
    'aria-label': "主导航",
}));
const __VLS_7 = __VLS_6({
    width: "220px",
    ...{ class: "aside" },
    role: "navigation",
    'aria-label': "主导航",
}, ...__VLS_functionalComponentArgsRest(__VLS_6));
__VLS_8.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "brand" },
});
(__VLS_ctx.activeStoreName || '按摩店');
const __VLS_9 = {}.ElMenu;
/** @type {[typeof __VLS_components.ElMenu, typeof __VLS_components.elMenu, typeof __VLS_components.ElMenu, typeof __VLS_components.elMenu, ]} */ ;
// @ts-ignore
const __VLS_10 = __VLS_asFunctionalComponent(__VLS_9, new __VLS_9({
    defaultActive: (__VLS_ctx.route.path),
    router: true,
    ...{ class: "menu" },
    backgroundColor: "#1f2d3d",
    textColor: "#bfcbd9",
    activeTextColor: "#ffd04b",
}));
const __VLS_11 = __VLS_10({
    defaultActive: (__VLS_ctx.route.path),
    router: true,
    ...{ class: "menu" },
    backgroundColor: "#1f2d3d",
    textColor: "#bfcbd9",
    activeTextColor: "#ffd04b",
}, ...__VLS_functionalComponentArgsRest(__VLS_10));
__VLS_12.slots.default;
for (const [item] of __VLS_getVForSourceType((__VLS_ctx.visibleMenu))) {
    const __VLS_13 = {}.ElMenuItem;
    /** @type {[typeof __VLS_components.ElMenuItem, typeof __VLS_components.elMenuItem, typeof __VLS_components.ElMenuItem, typeof __VLS_components.elMenuItem, ]} */ ;
    // @ts-ignore
    const __VLS_14 = __VLS_asFunctionalComponent(__VLS_13, new __VLS_13({
        index: (item.path),
        'aria-label': (item.title),
    }));
    const __VLS_15 = __VLS_14({
        index: (item.path),
        'aria-label': (item.title),
    }, ...__VLS_functionalComponentArgsRest(__VLS_14));
    __VLS_16.slots.default;
    if (item.icon) {
        const __VLS_17 = {}.ElIcon;
        /** @type {[typeof __VLS_components.ElIcon, typeof __VLS_components.elIcon, typeof __VLS_components.ElIcon, typeof __VLS_components.elIcon, ]} */ ;
        // @ts-ignore
        const __VLS_18 = __VLS_asFunctionalComponent(__VLS_17, new __VLS_17({}));
        const __VLS_19 = __VLS_18({}, ...__VLS_functionalComponentArgsRest(__VLS_18));
        __VLS_20.slots.default;
        const __VLS_21 = ((__VLS_ctx.iconCmp(item.icon)));
        // @ts-ignore
        const __VLS_22 = __VLS_asFunctionalComponent(__VLS_21, new __VLS_21({}));
        const __VLS_23 = __VLS_22({}, ...__VLS_functionalComponentArgsRest(__VLS_22));
        var __VLS_20;
    }
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    (item.title);
    var __VLS_16;
}
var __VLS_12;
var __VLS_8;
const __VLS_25 = {}.ElContainer;
/** @type {[typeof __VLS_components.ElContainer, typeof __VLS_components.elContainer, typeof __VLS_components.ElContainer, typeof __VLS_components.elContainer, ]} */ ;
// @ts-ignore
const __VLS_26 = __VLS_asFunctionalComponent(__VLS_25, new __VLS_25({}));
const __VLS_27 = __VLS_26({}, ...__VLS_functionalComponentArgsRest(__VLS_26));
__VLS_28.slots.default;
const __VLS_29 = {}.ElHeader;
/** @type {[typeof __VLS_components.ElHeader, typeof __VLS_components.elHeader, typeof __VLS_components.ElHeader, typeof __VLS_components.elHeader, ]} */ ;
// @ts-ignore
const __VLS_30 = __VLS_asFunctionalComponent(__VLS_29, new __VLS_29({
    ...{ class: "header" },
    role: "banner",
}));
const __VLS_31 = __VLS_30({
    ...{ class: "header" },
    role: "banner",
}, ...__VLS_functionalComponentArgsRest(__VLS_30));
__VLS_32.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "header-left" },
});
__VLS_asFunctionalElement(__VLS_intrinsicElements.h1, __VLS_intrinsicElements.h1)({
    ...{ class: "page-title" },
    'aria-live': "polite",
});
(__VLS_ctx.pageTitle);
if (__VLS_ctx.expireWarn) {
    const __VLS_33 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_34 = __VLS_asFunctionalComponent(__VLS_33, new __VLS_33({
        type: "warning",
        size: "small",
    }));
    const __VLS_35 = __VLS_34({
        type: "warning",
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_34));
    __VLS_36.slots.default;
    (__VLS_ctx.subStore.daysToExpire);
    var __VLS_36;
}
if (__VLS_ctx.subStore.expired) {
    const __VLS_37 = {}.ElTag;
    /** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
    // @ts-ignore
    const __VLS_38 = __VLS_asFunctionalComponent(__VLS_37, new __VLS_37({
        type: "danger",
        size: "small",
    }));
    const __VLS_39 = __VLS_38({
        type: "danger",
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_38));
    __VLS_40.slots.default;
    var __VLS_40;
}
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "header-right" },
});
if (__VLS_ctx.appStore.stores.length > 1) {
    const __VLS_41 = {}.ElSelect;
    /** @type {[typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, typeof __VLS_components.ElSelect, typeof __VLS_components.elSelect, ]} */ ;
    // @ts-ignore
    const __VLS_42 = __VLS_asFunctionalComponent(__VLS_41, new __VLS_41({
        ...{ 'onChange': {} },
        modelValue: (__VLS_ctx.appStore.activeStoreId),
        size: "small",
        ...{ style: {} },
        'aria-label': "切换门店",
    }));
    const __VLS_43 = __VLS_42({
        ...{ 'onChange': {} },
        modelValue: (__VLS_ctx.appStore.activeStoreId),
        size: "small",
        ...{ style: {} },
        'aria-label': "切换门店",
    }, ...__VLS_functionalComponentArgsRest(__VLS_42));
    let __VLS_45;
    let __VLS_46;
    let __VLS_47;
    const __VLS_48 = {
        onChange: ((v) => __VLS_ctx.appStore.setActiveStore(v))
    };
    __VLS_44.slots.default;
    for (const [s] of __VLS_getVForSourceType((__VLS_ctx.appStore.stores))) {
        const __VLS_49 = {}.ElOption;
        /** @type {[typeof __VLS_components.ElOption, typeof __VLS_components.elOption, ]} */ ;
        // @ts-ignore
        const __VLS_50 = __VLS_asFunctionalComponent(__VLS_49, new __VLS_49({
            key: (s.id),
            label: (s.name + (s.isHeadquarters ? '（总店）' : '')),
            value: (s.id),
        }));
        const __VLS_51 = __VLS_50({
            key: (s.id),
            label: (s.name + (s.isHeadquarters ? '（总店）' : '')),
            value: (s.id),
        }, ...__VLS_functionalComponentArgsRest(__VLS_50));
    }
    var __VLS_44;
}
const __VLS_53 = {}.ElDropdown;
/** @type {[typeof __VLS_components.ElDropdown, typeof __VLS_components.elDropdown, typeof __VLS_components.ElDropdown, typeof __VLS_components.elDropdown, ]} */ ;
// @ts-ignore
const __VLS_54 = __VLS_asFunctionalComponent(__VLS_53, new __VLS_53({
    ...{ 'onCommand': {} },
    trigger: "click",
}));
const __VLS_55 = __VLS_54({
    ...{ 'onCommand': {} },
    trigger: "click",
}, ...__VLS_functionalComponentArgsRest(__VLS_54));
let __VLS_57;
let __VLS_58;
let __VLS_59;
const __VLS_60 = {
    onCommand: (__VLS_ctx.onCommand)
};
__VLS_56.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
    ...{ class: "user" },
});
const __VLS_61 = {}.ElIcon;
/** @type {[typeof __VLS_components.ElIcon, typeof __VLS_components.elIcon, typeof __VLS_components.ElIcon, typeof __VLS_components.elIcon, ]} */ ;
// @ts-ignore
const __VLS_62 = __VLS_asFunctionalComponent(__VLS_61, new __VLS_61({}));
const __VLS_63 = __VLS_62({}, ...__VLS_functionalComponentArgsRest(__VLS_62));
__VLS_64.slots.default;
const __VLS_65 = {}.UserFilled;
/** @type {[typeof __VLS_components.UserFilled, ]} */ ;
// @ts-ignore
const __VLS_66 = __VLS_asFunctionalComponent(__VLS_65, new __VLS_65({}));
const __VLS_67 = __VLS_66({}, ...__VLS_functionalComponentArgsRest(__VLS_66));
var __VLS_64;
(__VLS_ctx.auth.user?.realName || __VLS_ctx.auth.user?.username);
const __VLS_69 = {}.ElTag;
/** @type {[typeof __VLS_components.ElTag, typeof __VLS_components.elTag, typeof __VLS_components.ElTag, typeof __VLS_components.elTag, ]} */ ;
// @ts-ignore
const __VLS_70 = __VLS_asFunctionalComponent(__VLS_69, new __VLS_69({
    size: "small",
    effect: "plain",
}));
const __VLS_71 = __VLS_70({
    size: "small",
    effect: "plain",
}, ...__VLS_functionalComponentArgsRest(__VLS_70));
__VLS_72.slots.default;
(__VLS_ctx.roleLabel);
var __VLS_72;
{
    const { dropdown: __VLS_thisSlot } = __VLS_56.slots;
    const __VLS_73 = {}.ElDropdownMenu;
    /** @type {[typeof __VLS_components.ElDropdownMenu, typeof __VLS_components.elDropdownMenu, typeof __VLS_components.ElDropdownMenu, typeof __VLS_components.elDropdownMenu, ]} */ ;
    // @ts-ignore
    const __VLS_74 = __VLS_asFunctionalComponent(__VLS_73, new __VLS_73({}));
    const __VLS_75 = __VLS_74({}, ...__VLS_functionalComponentArgsRest(__VLS_74));
    __VLS_76.slots.default;
    const __VLS_77 = {}.ElDropdownItem;
    /** @type {[typeof __VLS_components.ElDropdownItem, typeof __VLS_components.elDropdownItem, typeof __VLS_components.ElDropdownItem, typeof __VLS_components.elDropdownItem, ]} */ ;
    // @ts-ignore
    const __VLS_78 = __VLS_asFunctionalComponent(__VLS_77, new __VLS_77({
        command: "logout",
    }));
    const __VLS_79 = __VLS_78({
        command: "logout",
    }, ...__VLS_functionalComponentArgsRest(__VLS_78));
    __VLS_80.slots.default;
    var __VLS_80;
    var __VLS_76;
}
var __VLS_56;
var __VLS_32;
const __VLS_81 = {}.ElMain;
/** @type {[typeof __VLS_components.ElMain, typeof __VLS_components.elMain, typeof __VLS_components.ElMain, typeof __VLS_components.elMain, ]} */ ;
// @ts-ignore
const __VLS_82 = __VLS_asFunctionalComponent(__VLS_81, new __VLS_81({
    ...{ class: "main" },
    role: "main",
    id: "main-content",
    tabindex: "-1",
}));
const __VLS_83 = __VLS_82({
    ...{ class: "main" },
    role: "main",
    id: "main-content",
    tabindex: "-1",
}, ...__VLS_functionalComponentArgsRest(__VLS_82));
__VLS_84.slots.default;
const __VLS_85 = {}.RouterView;
/** @type {[typeof __VLS_components.RouterView, typeof __VLS_components.routerView, ]} */ ;
// @ts-ignore
const __VLS_86 = __VLS_asFunctionalComponent(__VLS_85, new __VLS_85({}));
const __VLS_87 = __VLS_86({}, ...__VLS_functionalComponentArgsRest(__VLS_86));
var __VLS_84;
var __VLS_28;
var __VLS_3;
/** @type {__VLS_StyleScopedClasses['layout']} */ ;
/** @type {__VLS_StyleScopedClasses['skip-link']} */ ;
/** @type {__VLS_StyleScopedClasses['aside']} */ ;
/** @type {__VLS_StyleScopedClasses['brand']} */ ;
/** @type {__VLS_StyleScopedClasses['menu']} */ ;
/** @type {__VLS_StyleScopedClasses['header']} */ ;
/** @type {__VLS_StyleScopedClasses['header-left']} */ ;
/** @type {__VLS_StyleScopedClasses['page-title']} */ ;
/** @type {__VLS_StyleScopedClasses['header-right']} */ ;
/** @type {__VLS_StyleScopedClasses['user']} */ ;
/** @type {__VLS_StyleScopedClasses['main']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            UserFilled: UserFilled,
            route: route,
            auth: auth,
            appStore: appStore,
            subStore: subStore,
            roleLabel: roleLabel,
            iconCmp: iconCmp,
            visibleMenu: visibleMenu,
            pageTitle: pageTitle,
            activeStoreName: activeStoreName,
            expireWarn: expireWarn,
            onCommand: onCommand,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
