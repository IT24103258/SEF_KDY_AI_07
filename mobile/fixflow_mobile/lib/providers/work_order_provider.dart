import 'package:flutter/material.dart';
import '../models/work_order_model.dart';
import '../services/work_order_service.dart';

class WorkOrderProvider with ChangeNotifier {
  final WorkOrderService _service;
  List<WorkOrderModel> _scheduleJobs = [];
  List<WorkOrderModel> _allJobs = [];
  WorkOrderModel? _currentJob;
  DateTime _selectedDate = DateTime.now();
  bool _isLoading = false;
  String? _error;

  WorkOrderProvider({WorkOrderService? service})
      : _service = service ?? WorkOrderService();

  List<WorkOrderModel> get scheduleJobs => _scheduleJobs;
  List<WorkOrderModel> get allJobs => _allJobs;
  WorkOrderModel? get currentJob => _currentJob;
  DateTime get selectedDate => _selectedDate;
  bool get isLoading => _isLoading;
  String? get error => _error;

  void setSelectedDate(DateTime date) {
    _selectedDate = date;
    fetchSchedule(filterDate: date);
  }

  Future<void> fetchSchedule({DateTime? filterDate}) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final date = filterDate ?? _selectedDate;
      _scheduleJobs = await _service.getTechnicianSchedule(filterDate: date);
    } catch (e) {
      _error = e.toString();
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<void> fetchAllJobs({String? status}) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _allJobs = await _service.getAllJobs(status: status);
    } catch (e) {
      _error = e.toString();
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<void> fetchJobDetails(String id) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _currentJob = await _service.getWorkOrderById(id);
    } catch (e) {
      _error = e.toString();
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<bool> startJob(String id) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final updated = await _service.updateStatus(id, 'InProgress', reason: 'Technician started work on site.');
      if (updated != null) {
        _currentJob = updated;
        await fetchSchedule();
        _isLoading = false;
        notifyListeners();
        return true;
      }
    } catch (e) {
      _error = e.toString();
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }

  Future<bool> pauseJob(String id, {String reason = ''}) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final updated = await _service.updateStatus(id, 'Paused', reason: reason.isNotEmpty ? reason : 'Job temporarily paused by technician.');
      if (updated != null) {
        _currentJob = updated;
        await fetchSchedule();
        _isLoading = false;
        notifyListeners();
        return true;
      }
    } catch (e) {
      _error = e.toString();
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }

  Future<bool> completeJob(
    String id, {
    required String signerName,
    required String signatureDataUrl,
    String? notes,
    String? photoFileKey,
  }) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final updated = await _service.completeWorkOrder(
        id,
        signerName: signerName,
        signatureDataUrl: signatureDataUrl,
        completionNotes: notes,
        photoFileKey: photoFileKey,
      );
      if (updated != null) {
        _currentJob = updated;
        await fetchSchedule();
        _isLoading = false;
        notifyListeners();
        return true;
      }
    } catch (e) {
      _error = e.toString();
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }

  Future<bool> addNote(String id, String text) async {
    try {
      final success = await _service.addNote(id, text);
      if (success) {
        await fetchJobDetails(id);
        return true;
      }
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
    return false;
  }
}
