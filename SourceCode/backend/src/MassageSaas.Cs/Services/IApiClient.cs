using MassageSaas.Shared.Appointments;
using MassageSaas.Shared.Auth;
using MassageSaas.Shared.Commissions;
using MassageSaas.Shared.Common;
using MassageSaas.Shared.DayCloses;
using MassageSaas.Shared.Members;
using MassageSaas.Shared.Orders;
using MassageSaas.Shared.Queue;
using MassageSaas.Shared.Reports;
using MassageSaas.Shared.Rooms;
using MassageSaas.Shared.Services;
using MassageSaas.Shared.Staff;
using MassageSaas.Shared.Stores;
using MassageSaas.Shared.Subscriptions;
using Refit;

namespace MassageSaas.Cs.Services;

public interface IApiClient
{
    [Post("/auth/login")]
    Task<LoginResponse> LoginAsync([Body] LoginRequest req);

    [Get("/services")]
    Task<List<ServiceItemDto>> GetServicesAsync([Query] bool includeInactive = false);

    [Post("/services")]
    Task<ServiceItemDto> CreateServiceAsync([Body] CreateServiceItemRequest req);

    [Put("/services/{id}")]
    Task<ServiceItemDto> UpdateServiceAsync(long id, [Body] UpdateServiceItemRequest req);

    [Delete("/services/{id}")]
    Task DeleteServiceAsync(long id);

    [Get("/stores")]
    Task<List<StoreDto>> GetStoresAsync();

    [Post("/stores")]
    Task<StoreDto> CreateStoreAsync([Body] CreateStoreRequest req);

    [Put("/stores/{id}")]
    Task<StoreDto> UpdateStoreAsync(long id, [Body] UpdateStoreRequest req);

    [Get("/staff")]
    Task<PagedResult<StaffDto>> GetStaffAsync(
        [Query] int page = 1,
        [Query] int pageSize = 50,
        [Query] string? role = null,
        [Query] long? storeId = null,
        [Query] string? keyword = null);

    [Post("/staff")]
    Task<StaffDto> CreateStaffAsync([Body] CreateStaffRequest req);

    [Put("/staff/{id}")]
    Task<StaffDto> UpdateStaffAsync(long id, [Body] UpdateStaffRequest req);

    [Post("/staff/{id}/reset-password")]
    Task ResetPasswordAsync(long id, [Body] ResetPasswordRequest req);

    [Get("/members")]
    Task<PagedResult<MemberDto>> GetMembersAsync(
        [Query] int page = 1,
        [Query] int pageSize = 20,
        [Query] string? keyword = null,
        [Query] long? storeId = null);

    [Get("/members/{id}")]
    Task<MemberDto> GetMemberAsync(long id);

    [Post("/members")]
    Task<MemberDto> CreateMemberAsync([Body] CreateMemberRequest req);

    [Put("/members/{id}")]
    Task<MemberDto> UpdateMemberAsync(long id, [Body] UpdateMemberRequest req);

    [Post("/members/recharge")]
    Task<RechargeRecordDto> RechargeAsync([Body] RechargeRequest req);

    [Get("/members/{id}/recharges")]
    Task<List<RechargeRecordDto>> GetRechargeHistoryAsync(long id);

    [Get("/orders")]
    Task<PagedResult<OrderListItemDto>> GetOrdersAsync(
        [Query] int page = 1,
        [Query] int pageSize = 20,
        [Query] long? storeId = null,
        [Query] string? status = null,
        [Query] DateTime? from = null,
        [Query] DateTime? to = null);

    [Get("/orders/{id}")]
    Task<OrderDto> GetOrderAsync(long id);

    [Post("/orders")]
    Task<OrderDto> CreateOrderAsync([Body] CreateOrderRequest req);

    [Post("/orders/{id}/checkout")]
    Task<OrderDto> CheckoutAsync(long id, [Body] CheckoutRequest req);

    [Post("/orders/{id}/refund")]
    Task<OrderDto> RefundAsync(long id, [Body] RefundRequest req);

    [Post("/orders/{id}/cancel")]
    Task CancelOrderAsync(long id);

    [Get("/queue")]
    Task<List<TechnicianQueueItemDto>> GetQueueAsync([Query] long storeId);

    [Post("/queue/{technicianId}/state")]
    Task SetQueueStateAsync(long technicianId, [Body] SetQueueStateRequest req);

    [Post("/queue/call-next")]
    Task<CallNextResultDto> CallNextAsync([Body] CallNextRequest req);

    [Post("/queue/reset-day")]
    Task ResetQueueDayAsync([Query] long storeId);

    [Get("/commission-rules")]
    Task<List<CommissionRuleDto>> GetCommissionRulesAsync(
        [Query] long? serviceId = null,
        [Query] long? technicianId = null);

    [Post("/commission-rules")]
    Task<CommissionRuleDto> CreateCommissionRuleAsync([Body] CreateCommissionRuleRequest req);

    [Put("/commission-rules/{id}")]
    Task<CommissionRuleDto> UpdateCommissionRuleAsync(long id, [Body] UpdateCommissionRuleRequest req);

    [Delete("/commission-rules/{id}")]
    Task DeleteCommissionRuleAsync(long id);

    [Get("/reports/daily")]
    Task<DailyReportDto> GetDailyReportAsync([Query] long storeId, [Query] DateTime? date = null);

    [Get("/reports/technician-performance")]
    Task<List<TechnicianPerformanceDto>> GetTechnicianPerformanceAsync(
        [Query] long storeId,
        [Query] DateTime from,
        [Query] DateTime to);

    [Get("/subscriptions/me")]
    Task<TenantSubscriptionStatusDto> GetMySubscriptionAsync();

    [Patch("/orders/{orderId}/items/{itemId}/transfer")]
    Task<OrderDto> TransferTechnicianAsync(long orderId, long itemId, [Body] TransferTechnicianRequest req);

    [Get("/appointments")]
    Task<PagedResult<AppointmentDto>> GetAppointmentsAsync(
        [Query] long? storeId = null,
        [Query] string? status = null,
        [Query] DateTime? from = null,
        [Query] DateTime? to = null,
        [Query] int page = 1,
        [Query] int pageSize = 20);

    [Post("/appointments/{id}/confirm")]
    Task<AppointmentDto> ConfirmAppointmentAsync(long id, [Body] ConfirmAppointmentRequest req);

    [Post("/appointments/{id}/arrive")]
    Task<AppointmentDto> ArriveAppointmentAsync(long id);

    [Post("/appointments/{id}/cancel")]
    Task<AppointmentDto> CancelAppointmentAsync(long id, [Body] CancelAppointmentRequest req);

    [Get("/rooms")]
    Task<List<RoomDto>> GetRoomsAsync([Query] long storeId, [Query] bool includeInactive = false);

    [Post("/rooms")]
    Task<RoomDto> CreateRoomAsync([Body] CreateRoomRequest req);

    [Put("/rooms/{id}")]
    Task<RoomDto> UpdateRoomAsync(long id, [Body] UpdateRoomRequest req);

    [Delete("/rooms/{id}")]
    Task DeleteRoomAsync(long id);

    [Get("/day-closes/preview")]
    Task<DayClosePreviewDto> GetDayClosePreviewAsync([Query] long storeId, [Query] DateTime? date = null);

    [Post("/day-closes")]
    Task<DayCloseDto> SubmitDayCloseAsync([Body] SubmitDayCloseRequest req);

    [Get("/day-closes")]
    Task<List<DayCloseDto>> GetDayCloseHistoryAsync(
        [Query] long storeId,
        [Query] DateTime? from = null,
        [Query] DateTime? to = null);
}
