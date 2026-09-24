import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:fixflow_mobile/models/work_order_model.dart';
import 'package:fixflow_mobile/providers/work_order_provider.dart';
import 'package:fixflow_mobile/services/work_order_service.dart';
import 'package:fixflow_mobile/screens/technician_schedule_screen.dart';

class MockWorkOrderService extends WorkOrderService {
  final List<WorkOrderModel> _mockJobs = [
    WorkOrderModel(
      id: 'wo-1',
      workOrderNumber: 'WO-202609-0001',
      title: 'Ceiling Leak Inspection',
      description: 'Check pipe leak in bathroom',
      requestId: 'req-1',
      requestNumber: 'REQ-2026-0001',
      technicianId: 'tech-1',
      technicianName: 'Senior Technician',
      locationName: 'Tower A Unit 305',
      building: 'Tower A',
      room: '305',
      priority: 'Critical',
      status: 'Scheduled',
      scheduledStartTime: DateTime.now().add(const Duration(hours: 1)),
      scheduledEndTime: DateTime.now().add(const Duration(hours: 3)),
      estimatedDurationMinutes: 120,
      conflictDetected: false,
      aiDecisionSummary: 'Optimal conflict-free slot identified within business hours.',
      notes: [
        WorkNoteModel(
          id: 'note-1',
          authorName: 'Tech',
          noteText: 'Tools prepared.',
          timestamp: DateTime.now(),
        ),
      ],
    ),
  ];

  @override
  Future<List<WorkOrderModel>> getTechnicianSchedule({DateTime? filterDate}) async {
    return _mockJobs;
  }

  @override
  Future<WorkOrderModel?> getWorkOrderById(String id) async {
    return _mockJobs.firstWhere((j) => j.id == id);
  }

  @override
  Future<WorkOrderModel?> updateStatus(String id, String status, {String reason = ''}) async {
    return _mockJobs.first;
  }
}

void main() {
  group('Component 4 Flutter WorkOrder Model Tests', () {
    test('WorkOrderModel.fromJson correctly parses full JSON payload', () {
      final json = {
        'id': 'wo-test-01',
        'workOrderNumber': 'WO-202609-0099',
        'title': 'HVAC Filter Replacement',
        'description': 'Replace lobby AC air filters',
        'requestId': 'req-99',
        'requestNumber': 'REQ-2026-0099',
        'technicianId': 'tech-01',
        'technicianName': 'Alex Silva',
        'locationName': 'Tower B Lobby',
        'building': 'Tower B',
        'room': 'Lobby',
        'priority': 'High',
        'status': 'Scheduled',
        'scheduledStartTime': '2026-09-24T09:00:00.000Z',
        'scheduledEndTime': '2026-09-24T11:00:00.000Z',
        'estimatedDurationMinutes': 120,
        'conflictDetected': false,
        'aiDecisionSummary': 'Conflict-free slot scheduled before SLA.',
        'notes': [
          {
            'id': 'note-1',
            'authorName': 'Alex',
            'noteText': 'Filters inspected.',
            'timestamp': '2026-09-24T09:10:00.000Z',
          }
        ]
      };

      final model = WorkOrderModel.fromJson(json);

      expect(model.id, 'wo-test-01');
      expect(model.workOrderNumber, 'WO-202609-0099');
      expect(model.title, 'HVAC Filter Replacement');
      expect(model.priority, 'High');
      expect(model.status, 'Scheduled');
      expect(model.estimatedDurationMinutes, 120);
      expect(model.conflictDetected, isFalse);
      expect(model.notes.length, 1);
      expect(model.notes.first.noteText, 'Filters inspected.');
    });

    test('WorkOrderModel.toJson preserves core fields', () {
      final model = WorkOrderModel(
        id: 'wo-1',
        workOrderNumber: 'WO-1',
        title: 'Title',
        description: 'Desc',
        requestId: 'req-1',
        requestNumber: 'REQ-1',
        technicianId: 'tech-1',
        technicianName: 'Tech',
        locationName: 'Loc',
        building: 'B',
        room: 'R',
        priority: 'Medium',
        status: 'Draft',
        estimatedDurationMinutes: 60,
        conflictDetected: true,
        aiDecisionSummary: '',
        notes: [],
      );

      final json = model.toJson();
      expect(json['id'], 'wo-1');
      expect(json['workOrderNumber'], 'WO-1');
      expect(json['conflictDetected'], isTrue);
    });
  });

  group('Component 4 Flutter WorkOrderProvider Tests', () {
    test('fetchSchedule populates scheduleJobs and clears error', () async {
      final mockService = MockWorkOrderService();
      final provider = WorkOrderProvider(service: mockService);

      expect(provider.scheduleJobs.isEmpty, isTrue);

      await provider.fetchSchedule();

      expect(provider.isLoading, isFalse);
      expect(provider.error, isNull);
      expect(provider.scheduleJobs.length, 1);
      expect(provider.scheduleJobs.first.workOrderNumber, 'WO-202609-0001');
    });
  });

  group('Component 4 TechnicianScheduleScreen Widget Tests', () {
    testWidgets('renders schedule screen with job card', (WidgetTester tester) async {
      final mockService = MockWorkOrderService();
      final provider = WorkOrderProvider(service: mockService);

      await tester.pumpWidget(
        MaterialApp(
          home: ChangeNotifierProvider<WorkOrderProvider>.value(
            value: provider,
            child: const TechnicianScheduleScreen(),
          ),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.text('My Daily Schedule'), findsOneWidget);
      expect(find.text('WO-202609-0001'), findsOneWidget);
      expect(find.text('Ceiling Leak Inspection'), findsOneWidget);
      expect(find.text('Scheduled'), findsOneWidget);
    });
  });
}
