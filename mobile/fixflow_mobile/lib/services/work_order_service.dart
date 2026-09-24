import '../core/network/api_client.dart';
import '../models/work_order_model.dart';

class WorkOrderService {
  final ApiClient _apiClient;

  WorkOrderService({ApiClient? apiClient}) : _apiClient = apiClient ?? ApiClient();

  // Get technician's assigned schedule
  Future<List<WorkOrderModel>> getTechnicianSchedule({DateTime? filterDate}) async {
    String endpoint = '/work-orders/technician/my-schedule';
    if (filterDate != null) {
      endpoint += '?filterDate=${filterDate.toIso8601String()}';
    }

    final res = await _apiClient.get(endpoint);
    if (res['success'] == true && res['data'] != null) {
      final List list = res['data'];
      return list.map((item) => WorkOrderModel.fromJson(item)).toList();
    }
    return [];
  }

  // Get single work order details with notes and timeline
  Future<WorkOrderModel?> getWorkOrderById(String id) async {
    final res = await _apiClient.get('/work-orders/$id');
    if (res['success'] == true && res['data'] != null) {
      return WorkOrderModel.fromJson(res['data']);
    }
    return null;
  }

  // Get all assigned jobs with status filter
  Future<List<WorkOrderModel>> getAllJobs({String? status}) async {
    String endpoint = '/work-orders';
    if (status != null && status.isNotEmpty) {
      endpoint += '?status=$status';
    }

    final res = await _apiClient.get(endpoint);
    if (res['success'] == true && res['data'] != null && res['data']['items'] != null) {
      final List list = res['data']['items'];
      return list.map((item) => WorkOrderModel.fromJson(item)).toList();
    }
    return [];
  }

  // Update status (e.g. InProgress, Paused)
  Future<WorkOrderModel?> updateStatus(String id, String status, {String reason = ''}) async {
    final res = await _apiClient.post('/work-orders/$id/status', {
      'status': status,
      'reason': reason,
    });
    if (res['success'] == true && res['data'] != null) {
      return WorkOrderModel.fromJson(res['data']);
    }
    return null;
  }

  // Add field note
  Future<bool> addNote(String workOrderId, String noteText) async {
    final res = await _apiClient.post('/work-orders/$workOrderId/notes', {
      'noteText': noteText,
    });
    return res['success'] == true;
  }

  // Submit completion evidence with customer signature
  Future<WorkOrderModel?> completeWorkOrder(
    String id, {
    required String signerName,
    required String signatureDataUrl,
    String? completionNotes,
    String? photoFileKey,
  }) async {
    final res = await _apiClient.post('/work-orders/$id/complete', {
      'signerName': signerName,
      'signatureDataUrl': signatureDataUrl,
      'completionNotes': completionNotes ?? 'Completed and signed on mobile.',
      'photoFileKey': photoFileKey,
    });
    if (res['success'] == true && res['data'] != null) {
      return WorkOrderModel.fromJson(res['data']);
    }
    return null;
  }
}
