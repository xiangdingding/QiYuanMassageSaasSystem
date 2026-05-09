import { api } from '../../../utils/api';

interface LoginResp {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: {
    id: number;
    username: string;
    realName?: string;
    role: string;
    tenantId?: number;
    storeId?: number;
    isBlind: boolean;
  };
}

Page({
  data: {
    username: '',
    password: '',
    tenantCode: '',
    loading: false,
    error: ''
  },
  onUsernameInput(e: WechatMiniprogram.Input) {
    this.setData({ username: e.detail.value, error: '' });
  },
  onPasswordInput(e: WechatMiniprogram.Input) {
    this.setData({ password: e.detail.value, error: '' });
  },
  onTenantInput(e: WechatMiniprogram.Input) {
    this.setData({ tenantCode: e.detail.value, error: '' });
  },
  async submit() {
    const { username, password, tenantCode } = this.data;
    if (!username || !password) {
      this.setData({ error: '请输入用户名和密码' });
      return;
    }
    this.setData({ loading: true, error: '' });
    try {
      const resp = await api.post<LoginResp>('/auth/login',
        { username, password, tenantCode: tenantCode || undefined },
        { noAuth: true });
      wx.setStorageSync('token', resp.accessToken);
      wx.setStorageSync('user', resp.user);
      wx.showToast({ title: `欢迎，${resp.user.realName || resp.user.username}`, icon: 'none' });
      // 登录后跳到我的班次页
      wx.switchTab({ url: '/pages/tech/home/home' });
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : '登录失败';
      this.setData({ error: msg });
    } finally {
      this.setData({ loading: false });
    }
  }
});
