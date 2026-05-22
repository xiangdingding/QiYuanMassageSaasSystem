import { reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { tenantsApi } from '@/api/modules';
const router = useRouter();
const form = reactive({
    name: '',
    contactPhone: '',
    contactName: '',
    ownerPhone: '',
    ownerPassword: '',
    confirmPassword: '',
    ownerRealName: ''
});
const formRef = ref();
const loading = ref(false);
const rules = {
    name: [{ required: true, message: '请输入店铺名', trigger: 'blur' }],
    contactPhone: [
        { required: true, message: '请输入联系电话', trigger: 'blur' },
        { pattern: /^\d{11}$/, message: '请输入 11 位手机号', trigger: 'blur' }
    ],
    ownerPhone: [
        { required: true, message: '请输入登录手机号', trigger: 'blur' },
        { pattern: /^\d{11}$/, message: '请输入 11 位手机号', trigger: 'blur' }
    ],
    ownerPassword: [
        { required: true, message: '请设置密码', trigger: 'blur' },
        { min: 6, message: '密码至少 6 位', trigger: 'blur' }
    ],
    confirmPassword: [
        { required: true, message: '请再次输入密码', trigger: 'blur' },
        {
            validator: (_rule, val, cb) => {
                if (val !== form.ownerPassword)
                    cb(new Error('两次密码不一致'));
                else
                    cb();
            },
            trigger: 'blur'
        }
    ]
};
async function submit() {
    if (!formRef.value)
        return;
    const ok = await formRef.value.validate().catch(() => false);
    if (!ok)
        return;
    loading.value = true;
    try {
        const resp = await tenantsApi.register({
            name: form.name,
            contactPhone: form.contactPhone,
            contactName: form.contactName || null,
            ownerPhone: form.ownerPhone,
            ownerPassword: form.ownerPassword,
            ownerRealName: form.ownerRealName || null
        });
        ElMessage.success('注册成功');
        await ElMessageBox.alert(`手机号「${resp.ownerPhone}」已开通 ${resp.trialDays} 天试用，试用至 ${new Date(resp.expireAt).toLocaleDateString('zh-CN')}。即将跳转到登录页，请使用手机号 + 密码登录。`, '欢迎使用', { confirmButtonText: '去登录', type: 'success' }).catch(() => null);
        router.replace({ path: '/login', query: { u: resp.ownerPhone } });
    }
    finally {
        loading.value = false;
    }
}
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
/** @type {__VLS_StyleScopedClasses['register-card']} */ ;
/** @type {__VLS_StyleScopedClasses['footer-link']} */ ;
// CSS variable injection 
// CSS variable injection end 
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "register-bg" },
});
const __VLS_0 = {}.ElCard;
/** @type {[typeof __VLS_components.ElCard, typeof __VLS_components.elCard, typeof __VLS_components.ElCard, typeof __VLS_components.elCard, ]} */ ;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({
    ...{ class: "register-card" },
}));
const __VLS_2 = __VLS_1({
    ...{ class: "register-card" },
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
__VLS_3.slots.default;
__VLS_asFunctionalElement(__VLS_intrinsicElements.h1, __VLS_intrinsicElements.h1)({});
__VLS_asFunctionalElement(__VLS_intrinsicElements.p, __VLS_intrinsicElements.p)({
    ...{ class: "subtitle" },
});
const __VLS_4 = {}.ElForm;
/** @type {[typeof __VLS_components.ElForm, typeof __VLS_components.elForm, typeof __VLS_components.ElForm, typeof __VLS_components.elForm, ]} */ ;
// @ts-ignore
const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
    ...{ 'onSubmit': {} },
    model: (__VLS_ctx.form),
    rules: (__VLS_ctx.rules),
    ref: "formRef",
    labelWidth: "100px",
}));
const __VLS_6 = __VLS_5({
    ...{ 'onSubmit': {} },
    model: (__VLS_ctx.form),
    rules: (__VLS_ctx.rules),
    ref: "formRef",
    labelWidth: "100px",
}, ...__VLS_functionalComponentArgsRest(__VLS_5));
let __VLS_8;
let __VLS_9;
let __VLS_10;
const __VLS_11 = {
    onSubmit: () => { }
};
/** @type {typeof __VLS_ctx.formRef} */ ;
var __VLS_12 = {};
__VLS_7.slots.default;
const __VLS_14 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_15 = __VLS_asFunctionalComponent(__VLS_14, new __VLS_14({
    label: "店铺名",
    prop: "name",
}));
const __VLS_16 = __VLS_15({
    label: "店铺名",
    prop: "name",
}, ...__VLS_functionalComponentArgsRest(__VLS_15));
__VLS_17.slots.default;
const __VLS_18 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_19 = __VLS_asFunctionalComponent(__VLS_18, new __VLS_18({
    modelValue: (__VLS_ctx.form.name),
    placeholder: "如：齐源按摩中心",
}));
const __VLS_20 = __VLS_19({
    modelValue: (__VLS_ctx.form.name),
    placeholder: "如：齐源按摩中心",
}, ...__VLS_functionalComponentArgsRest(__VLS_19));
var __VLS_17;
const __VLS_22 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_23 = __VLS_asFunctionalComponent(__VLS_22, new __VLS_22({
    label: "店铺电话",
    prop: "contactPhone",
}));
const __VLS_24 = __VLS_23({
    label: "店铺电话",
    prop: "contactPhone",
}, ...__VLS_functionalComponentArgsRest(__VLS_23));
__VLS_25.slots.default;
const __VLS_26 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_27 = __VLS_asFunctionalComponent(__VLS_26, new __VLS_26({
    modelValue: (__VLS_ctx.form.contactPhone),
    placeholder: "11 位手机号",
}));
const __VLS_28 = __VLS_27({
    modelValue: (__VLS_ctx.form.contactPhone),
    placeholder: "11 位手机号",
}, ...__VLS_functionalComponentArgsRest(__VLS_27));
var __VLS_25;
const __VLS_30 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_31 = __VLS_asFunctionalComponent(__VLS_30, new __VLS_30({
    label: "联系人",
}));
const __VLS_32 = __VLS_31({
    label: "联系人",
}, ...__VLS_functionalComponentArgsRest(__VLS_31));
__VLS_33.slots.default;
const __VLS_34 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_35 = __VLS_asFunctionalComponent(__VLS_34, new __VLS_34({
    modelValue: (__VLS_ctx.form.contactName),
    placeholder: "选填",
}));
const __VLS_36 = __VLS_35({
    modelValue: (__VLS_ctx.form.contactName),
    placeholder: "选填",
}, ...__VLS_functionalComponentArgsRest(__VLS_35));
var __VLS_33;
const __VLS_38 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_39 = __VLS_asFunctionalComponent(__VLS_38, new __VLS_38({
    label: "登录手机号",
    prop: "ownerPhone",
}));
const __VLS_40 = __VLS_39({
    label: "登录手机号",
    prop: "ownerPhone",
}, ...__VLS_functionalComponentArgsRest(__VLS_39));
__VLS_41.slots.default;
const __VLS_42 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_43 = __VLS_asFunctionalComponent(__VLS_42, new __VLS_42({
    modelValue: (__VLS_ctx.form.ownerPhone),
    placeholder: "店长/店员都用手机号登录",
}));
const __VLS_44 = __VLS_43({
    modelValue: (__VLS_ctx.form.ownerPhone),
    placeholder: "店长/店员都用手机号登录",
}, ...__VLS_functionalComponentArgsRest(__VLS_43));
var __VLS_41;
const __VLS_46 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_47 = __VLS_asFunctionalComponent(__VLS_46, new __VLS_46({
    label: "登录密码",
    prop: "ownerPassword",
}));
const __VLS_48 = __VLS_47({
    label: "登录密码",
    prop: "ownerPassword",
}, ...__VLS_functionalComponentArgsRest(__VLS_47));
__VLS_49.slots.default;
const __VLS_50 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_51 = __VLS_asFunctionalComponent(__VLS_50, new __VLS_50({
    modelValue: (__VLS_ctx.form.ownerPassword),
    type: "password",
    showPassword: true,
    placeholder: "至少 6 位",
}));
const __VLS_52 = __VLS_51({
    modelValue: (__VLS_ctx.form.ownerPassword),
    type: "password",
    showPassword: true,
    placeholder: "至少 6 位",
}, ...__VLS_functionalComponentArgsRest(__VLS_51));
var __VLS_49;
const __VLS_54 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_55 = __VLS_asFunctionalComponent(__VLS_54, new __VLS_54({
    label: "确认密码",
    prop: "confirmPassword",
}));
const __VLS_56 = __VLS_55({
    label: "确认密码",
    prop: "confirmPassword",
}, ...__VLS_functionalComponentArgsRest(__VLS_55));
__VLS_57.slots.default;
const __VLS_58 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_59 = __VLS_asFunctionalComponent(__VLS_58, new __VLS_58({
    ...{ 'onKeyup': {} },
    modelValue: (__VLS_ctx.form.confirmPassword),
    type: "password",
    showPassword: true,
    placeholder: "再次输入",
}));
const __VLS_60 = __VLS_59({
    ...{ 'onKeyup': {} },
    modelValue: (__VLS_ctx.form.confirmPassword),
    type: "password",
    showPassword: true,
    placeholder: "再次输入",
}, ...__VLS_functionalComponentArgsRest(__VLS_59));
let __VLS_62;
let __VLS_63;
let __VLS_64;
const __VLS_65 = {
    onKeyup: (__VLS_ctx.submit)
};
var __VLS_61;
var __VLS_57;
const __VLS_66 = {}.ElFormItem;
/** @type {[typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, typeof __VLS_components.ElFormItem, typeof __VLS_components.elFormItem, ]} */ ;
// @ts-ignore
const __VLS_67 = __VLS_asFunctionalComponent(__VLS_66, new __VLS_66({
    label: "您的姓名",
}));
const __VLS_68 = __VLS_67({
    label: "您的姓名",
}, ...__VLS_functionalComponentArgsRest(__VLS_67));
__VLS_69.slots.default;
const __VLS_70 = {}.ElInput;
/** @type {[typeof __VLS_components.ElInput, typeof __VLS_components.elInput, ]} */ ;
// @ts-ignore
const __VLS_71 = __VLS_asFunctionalComponent(__VLS_70, new __VLS_70({
    modelValue: (__VLS_ctx.form.ownerRealName),
    placeholder: "选填",
}));
const __VLS_72 = __VLS_71({
    modelValue: (__VLS_ctx.form.ownerRealName),
    placeholder: "选填",
}, ...__VLS_functionalComponentArgsRest(__VLS_71));
var __VLS_69;
const __VLS_74 = {}.ElButton;
/** @type {[typeof __VLS_components.ElButton, typeof __VLS_components.elButton, typeof __VLS_components.ElButton, typeof __VLS_components.elButton, ]} */ ;
// @ts-ignore
const __VLS_75 = __VLS_asFunctionalComponent(__VLS_74, new __VLS_74({
    ...{ 'onClick': {} },
    type: "primary",
    size: "large",
    loading: (__VLS_ctx.loading),
    ...{ style: {} },
}));
const __VLS_76 = __VLS_75({
    ...{ 'onClick': {} },
    type: "primary",
    size: "large",
    loading: (__VLS_ctx.loading),
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_75));
let __VLS_78;
let __VLS_79;
let __VLS_80;
const __VLS_81 = {
    onClick: (__VLS_ctx.submit)
};
__VLS_77.slots.default;
var __VLS_77;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "footer-link" },
});
const __VLS_82 = {}.RouterLink;
/** @type {[typeof __VLS_components.RouterLink, typeof __VLS_components.routerLink, typeof __VLS_components.RouterLink, typeof __VLS_components.routerLink, ]} */ ;
// @ts-ignore
const __VLS_83 = __VLS_asFunctionalComponent(__VLS_82, new __VLS_82({
    to: "/login",
}));
const __VLS_84 = __VLS_83({
    to: "/login",
}, ...__VLS_functionalComponentArgsRest(__VLS_83));
__VLS_85.slots.default;
var __VLS_85;
var __VLS_7;
var __VLS_3;
/** @type {__VLS_StyleScopedClasses['register-bg']} */ ;
/** @type {__VLS_StyleScopedClasses['register-card']} */ ;
/** @type {__VLS_StyleScopedClasses['subtitle']} */ ;
/** @type {__VLS_StyleScopedClasses['footer-link']} */ ;
// @ts-ignore
var __VLS_13 = __VLS_12;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            form: form,
            formRef: formRef,
            loading: loading,
            rules: rules,
            submit: submit,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
