using MassageSaas.Shared.Appointments;
using MassageSaas.Shared.Auth;
using MassageSaas.Shared.Commissions;
using MassageSaas.Shared.Common;
using MassageSaas.Shared.Complaints;
using MassageSaas.Shared.DayCloses;
using MassageSaas.Shared.Inventory;
using MassageSaas.Shared.MemberPackages;
using MassageSaas.Shared.Members;
using MassageSaas.Shared.Orders;
using MassageSaas.Shared.Payroll;
using MassageSaas.Shared.Queue;
using MassageSaas.Shared.Reports;
using MassageSaas.Shared.Reviews;
using MassageSaas.Shared.Rooms;
using MassageSaas.Shared.Schedules;
using MassageSaas.Shared.ServicePackages;
using MassageSaas.Shared.Services;
using MassageSaas.Shared.Staff;
using MassageSaas.Shared.Stores;
using MassageSaas.Shared.Subscriptions;
using MassageSaas.Shared.Vouchers;
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

    [Get("/staff/transfers")]
    Task<List<StaffTransferDto>> GetStaffTransfersAsync(
        [Query] long? userId = null,
        [Query] long? storeId = null,
        [Query] string? status = null);

    [Post("/staff/{id}/transfer")]
    Task<StaffTransferDto> TransferStaffAsync(long id, [Body] TransferStaffRequest req);

    [Post("/staff/transfers/{transferId}/return")]
    Task<StaffTransferDto> ReturnStaffTransferAsync(long transferId);

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

    [Post("/members/{id}/refund")]
    Task<RechargeRecordDto> RefundMemberAsync(long id, [Body] RefundMemberRequest req);

    [Post("/members/{id}/transfer")]
    Task<MemberDto> TransferMemberAsync(long id, [Body] TransferMemberRequest req);

    [Get("/members/{id}/referrals")]
    Task<ReferralSummaryDto> GetMemberReferralsAsync(long id);

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

    [Get("/queue/me")]
    Task<MyQueueDto> GetMyQueueAsync();

    [Post("/queue/me/state")]
    Task SetMyQueueStateAsync([Body] SetQueueStateRequest req);

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

    [Post("/orders/items/merge")]
    Task<List<long>> MergeOrderItemsAsync([Body] MergeOrderItemsRequest req);

    [Post("/orders/items/{itemId}/unmerge")]
    Task UnmergeOrderItemAsync(long itemId);

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

    [Get("/timed-rooms/sessions")]
    Task<List<TimedRoomSessionDto>> GetTimedRoomSessionsAsync(
        [Query] long storeId,
        [Query] string? status = null,
        [Query] DateTime? from = null,
        [Query] DateTime? to = null);

    [Post("/timed-rooms/{roomId}/start")]
    Task<TimedRoomSessionDto> StartTimedRoomAsync(long roomId, [Body] StartTimedRoomRequest req);

    [Post("/timed-rooms/sessions/{id}/stop")]
    Task<TimedRoomSessionDto> StopTimedRoomAsync(long id, [Body] StopTimedRoomRequest req);

    [Post("/timed-rooms/sessions/{id}/cancel")]
    Task<TimedRoomSessionDto> CancelTimedRoomAsync(long id);

    [Get("/day-closes/preview")]
    Task<DayClosePreviewDto> GetDayClosePreviewAsync([Query] long storeId, [Query] DateTime? date = null);

    [Post("/day-closes")]
    Task<DayCloseDto> SubmitDayCloseAsync([Body] SubmitDayCloseRequest req);

    [Get("/day-closes")]
    Task<List<DayCloseDto>> GetDayCloseHistoryAsync(
        [Query] long storeId,
        [Query] DateTime? from = null,
        [Query] DateTime? to = null);

    [Post("/orders/{id}/items")]
    Task<OrderDto> AddOrderItemsAsync(long id, [Body] AddOrderItemsRequest req);

    [Post("/orders/{id}/reopen")]
    Task<OrderDto> ReopenOrderAsync(long id, [Body] ReopenOrderRequest req);

    [Post("/orders/{id}/tip")]
    Task<OrderDto> SetOrderTipAsync(long id, [Body] SetTipRequest req);

    [Get("/vouchers")]
    Task<List<VoucherDto>> GetVouchersAsync(
        [Query] string? status = null,
        [Query] string? keyword = null);

    [Post("/vouchers")]
    Task<VoucherDto> CreateVoucherAsync([Body] CreateVoucherRequest req);

    [Post("/vouchers/redeem")]
    Task<VoucherDto> RedeemVoucherAsync([Body] VoucherRedeemRequest req);

    [Post("/vouchers/{id}/cancel")]
    Task CancelVoucherAsync(long id);

    [Get("/member-packages")]
    Task<List<MemberPackageDto>> GetMemberPackagesAsync(
        [Query] long? memberId = null,
        [Query] long? storeId = null,
        [Query] string? status = null);

    [Post("/member-packages")]
    Task<MemberPackageDto> CreateMemberPackageAsync([Body] CreateMemberPackageRequest req);

    [Post("/member-packages/{id}/cancel")]
    Task CancelMemberPackageAsync(long id);

    [Get("/service-packages")]
    Task<List<ServicePackageDto>> GetServicePackagesAsync([Query] bool includeInactive = false);

    [Post("/service-packages")]
    Task<ServicePackageDto> CreateServicePackageAsync([Body] CreateServicePackageRequest req);

    [Put("/service-packages/{id}")]
    Task<ServicePackageDto> UpdateServicePackageAsync(long id, [Body] UpdateServicePackageRequest req);

    [Delete("/service-packages/{id}")]
    Task DeleteServicePackageAsync(long id);

    [Get("/inventory/items")]
    Task<List<InventoryItemDto>> GetInventoryAsync(
        [Query] long storeId,
        [Query] bool onlyLowStock = false);

    [Post("/inventory/items")]
    Task<InventoryItemDto> CreateInventoryItemAsync([Body] CreateInventoryItemRequest req);

    [Put("/inventory/items/{id}")]
    Task<InventoryItemDto> UpdateInventoryItemAsync(long id, [Body] UpdateInventoryItemRequest req);

    [Get("/inventory/movements")]
    Task<List<InventoryMovementDto>> GetInventoryMovementsAsync(
        [Query] long itemId,
        [Query] int take = 50);

    [Post("/inventory/movements")]
    Task<InventoryMovementDto> CreateInventoryMovementAsync([Body] CreateMovementRequest req);

    [Get("/reviews")]
    Task<List<ServiceReviewDto>> GetReviewsAsync(
        [Query] long? technicianId = null,
        [Query] int? rating = null,
        [Query] DateTime? from = null,
        [Query] DateTime? to = null);

    [Get("/reviews/technician-summary")]
    Task<List<TechnicianReviewSummaryDto>> GetReviewSummaryAsync(
        [Query] DateTime? from = null,
        [Query] DateTime? to = null);

    [Get("/schedules")]
    Task<List<StaffScheduleDto>> GetSchedulesAsync(
        [Query] long storeId,
        [Query] DateTime? from = null,
        [Query] DateTime? to = null);

    [Post("/schedules")]
    Task<StaffScheduleDto> CreateScheduleAsync([Body] CreateStaffScheduleRequest req);

    [Delete("/schedules/{id}")]
    Task DeleteScheduleAsync(long id);

    [Get("/schedules/leaves")]
    Task<List<LeaveRequestDto>> GetLeavesAsync(
        [Query] long? userId = null,
        [Query] string? status = null);

    [Post("/schedules/leaves")]
    Task<LeaveRequestDto> SubmitLeaveAsync([Body] CreateLeaveRequest req);

    [Post("/schedules/leaves/{id}/approve")]
    Task<LeaveRequestDto> ApproveLeaveAsync(long id, [Body] ApproveLeaveRequest req);

    [Get("/payroll/profiles")]
    Task<List<SalaryProfileDto>> GetSalaryProfilesAsync([Query] long? storeId = null);

    [Get("/payroll/profiles/{userId}")]
    Task<SalaryProfileDto> GetSalaryProfileAsync(long userId);

    [Put("/payroll/profiles/{userId}")]
    Task<SalaryProfileDto> UpsertSalaryProfileAsync(long userId, [Body] UpsertSalaryProfileRequest req);

    [Get("/payroll/periods")]
    Task<List<PayrollPeriodDto>> GetPayrollPeriodsAsync([Query] long storeId, [Query] int? year = null);

    [Get("/payroll/periods/{id}")]
    Task<PayrollPeriodDetailDto> GetPayrollPeriodAsync(long id);

    [Post("/payroll/periods")]
    Task<PayrollPeriodDetailDto> GeneratePayrollAsync([Body] GeneratePayrollRequest req);

    [Post("/payroll/periods/{id}/lock")]
    Task<PayrollPeriodDto> LockPayrollAsync(long id, [Body] LockPayrollRequest req);

    [Post("/payroll/periods/{id}/mark-paid")]
    Task<PayrollPeriodDto> MarkPayrollPaidAsync(long id);

    [Delete("/payroll/periods/{id}")]
    Task DeletePayrollDraftAsync(long id);

    [Patch("/payroll/items/{id}")]
    Task<PayrollItemDto> UpdatePayrollItemAsync(long id, [Body] UpdatePayrollItemRequest req);

    [Post("/payroll/items/{id}/adjustments")]
    Task<PayrollItemDto> AddPayrollAdjustmentAsync(long id, [Body] AddAdjustmentRequest req);

    [Delete("/payroll/items/{itemId}/adjustments/{adjId}")]
    Task<PayrollItemDto> RemovePayrollAdjustmentAsync(long itemId, long adjId);

    [Get("/payroll/me")]
    Task<List<PayrollItemDto>> GetMyPayrollAsync([Query] int take = 6);

    [Get("/complaints")]
    Task<PagedResult<ComplaintDto>> GetComplaintsAsync(
        [Query] long? storeId = null,
        [Query] long? technicianId = null,
        [Query] string? status = null,
        [Query] DateTime? from = null,
        [Query] DateTime? to = null,
        [Query] int page = 1,
        [Query] int pageSize = 20);

    [Get("/complaints/{id}")]
    Task<ComplaintDto> GetComplaintAsync(long id);

    [Post("/complaints")]
    Task<ComplaintDto> CreateComplaintAsync([Body] CreateComplaintRequest req);

    [Patch("/complaints/{id}/resolve")]
    Task<ComplaintDto> ResolveComplaintAsync(long id, [Body] ResolveComplaintRequest req);

    [Post("/complaints/{id}/cancel")]
    Task<ComplaintDto> CancelComplaintAsync(long id);
}
