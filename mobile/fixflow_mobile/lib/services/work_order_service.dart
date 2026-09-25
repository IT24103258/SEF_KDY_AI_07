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

  // Get all assigned jobs with status, search, and priority filter
  Future<List<WorkOrderModel>> getAllJobs({
    String? status,
    String? search,
    String? priority,
    int page = 1,
    int pageSize = 50,
  }) async {
    final queryParams = <String>[];
    queryParams.add('page=$page');
    queryParams.add('pageSize=$pageSize');
    if (status != null && status.isNotEmpty) {
      queryParams.add('status=$status');
    }
    if (search != null && search.isNotEmpty) {
      queryParams.add('search=${Uri.encodeComponent(search)}');
    }
    if (priority != null && priority.isNotEmpty) {
      queryParams.add('priority=$priority');
    }

    final endpoint = '/work-orders?${queryParams.join('&')}';

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

  // Upload photo evidence
  Future<String?> uploadPhoto(String workOrderId, List<int> fileBytes, String filename) async {
    try {
      final res = await _apiClient.uploadMultipart(
        '/work-orders/$workOrderId/evidence/photo',
        fileBytes: fileBytes,
        filename: filename,
        fieldName: 'file',
      );
      if (res['success'] == true && res['data'] != null) {
        return res['data'].toString();
      }
    } catch (_) {}
    return null;
  }

  // Submit completion evidence with customer signature
  Future<WorkOrderModel?> completeWorkOrder(
    String id, {
    required String signerName,
    required String signatureDataUrl,
    String? completionNotes,
    String? photoFileKey,
    String? photoOriginalFileName,
  }) async {
    final res = await _apiClient.post('/work-orders/$id/complete', {
      'signerName': signerName,
      'signatureDataUrl': signatureDataUrl,
      'completionNotes': completionNotes ?? 'Completed and signed on mobile.',
      'photoFileKey': photoFileKey,
      'photoOriginalFileName': photoOriginalFileName,
    });
    if (res['success'] == true && res['data'] != null) {
      return WorkOrderModel.fromJson(res['data']);
    }
    return null;
  }
}
