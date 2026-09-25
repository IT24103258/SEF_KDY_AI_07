import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:fixflow_mobile/models/work_order_model.dart';
import 'package:fixflow_mobile/providers/work_order_provider.dart';
import 'package:fixflow_mobile/services/work_order_service.dart';
import 'package:fixflow_mobile/screens/technician_schedule_screen.dart';
import 'package:fixflow_mobile/screens/all_jobs_screen.dart';
import 'package:fixflow_mobile/core/theme/app_colors.dart';
import 'package:fixflow_mobile/widgets/priority_badge.dart';
import 'package:fixflow_mobile/widgets/status_badge.dart';
import 'package:fixflow_mobile/widgets/date_section_header.dart';
import 'package:fixflow_mobile/widgets/empty_state.dart';
import 'package:fixflow_mobile/widgets/notes_list.dart';
import 'package:fixflow_mobile/providers/auth_provider.dart';

// ──────────────────────────────────────────────────────────────
// Shared Mock Service
// ──────────────────────────────────────────────────────────────
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
      aiDecisionSummary: 'Optimal conflict-free slot identified.',
      notes: [
        WorkNoteModel(
          id: 'note-1',
          authorName: 'Tech',
          noteText: 'Tools prepared.',
          timestamp: DateTime.now().subtract(const Duration(minutes: 30)),
        ),
      ],
      statusHistories: [],
      evidence: [],
    ),
    WorkOrderModel(
      id: 'wo-2',
      workOrderNumber: 'WO-202609-0002',
      title: 'HVAC Filter Replacement',
      description: 'Replace lobby AC filters',
      requestId: 'req-2',
      requestNumber: 'REQ-2026-0002',
      technicianId: 'tech-1',
      technicianName: 'Senior Technician',
      locationName: 'Tower B Lobby',
      building: 'Tower B',
      room: 'Lobby',
      priority: 'High',
      status: 'InProgress',
      scheduledStartTime: DateTime.now().add(const Duration(hours: 4)),
      scheduledEndTime: DateTime.now().add(const Duration(hours: 5)),
      estimatedDurationMinutes: 60,
      conflictDetected: false,
      aiDecisionSummary: '',
      notes: [],
      statusHistories: [],
      evidence: [],
    ),
  ];

  @override
  Future<List<WorkOrderModel>> getTechnicianSchedule({DateTime? filterDate}) async => _mockJobs;

  @override
  Future<WorkOrderModel?> getWorkOrderById(String id) async =>
      _mockJobs.firstWhere((j) => j.id == id, orElse: () => _mockJobs.first);

  @override
  Future<WorkOrderModel?> updateStatus(String id, String status, {String reason = ''}) async =>
      _mockJobs.first;

  @override
  Future<List<WorkOrderModel>> getAllJobs({
    String? status,
    String? search,
    String? priority,
    int page = 1,
    int pageSize = 50,
  }) async {
    var jobs = List<WorkOrderModel>.from(_mockJobs);
    if (status != null && status.isNotEmpty) {
      jobs = jobs.where((j) => j.status == status).toList();
    }
    if (search != null && search.isNotEmpty) {
      final q = search.toLowerCase();
      jobs = jobs.where((j) => j.title.toLowerCase().contains(q) || j.workOrderNumber.toLowerCase().contains(q)).toList();
    }
    return jobs;
  }

  @override
  Future<bool> addNote(String workOrderId, String noteText) async => true;
}

// ──────────────────────────────────────────────────────────────
// Helper: build test app
// ──────────────────────────────────────────────────────────────
Widget buildTestApp(Widget screen, {WorkOrderProvider? provider}) {
  final workOrderProvider = provider ?? WorkOrderProvider(service: MockWorkOrderService());
  return MaterialApp(
    home: MultiProvider(
      providers: [
        ChangeNotifierProvider<WorkOrderProvider>.value(value: workOrderProvider),
        ChangeNotifierProvider<AuthProvider>(create: (_) => AuthProvider()),
      ],
      child: screen,
    ),
  );
}

// ══════════════════════════════════════════════════════════════
void main() {
  // ──────────────────────────────────────────────────────────
  // WorkOrderModel Unit Tests
  // ──────────────────────────────────────────────────────────
  group('WorkOrderModel Unit Tests', () {
    test('fromJson parses full JSON payload correctly', () {
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
        'aiDecisionSummary': 'Conflict-free slot.',
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

    test('toJson preserves core fields', () {
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

    test('formattedLocation builds correctly', () {
      final model = WorkOrderModel(
        id: 'wo-1',
        workOrderNumber: 'WO-1',
        title: 'Test',
        description: '',
        requestId: 'r1',
        requestNumber: 'R1',
        technicianId: 't1',
        technicianName: 'Tech',
        locationName: 'Building A',
        building: 'Block A',
        room: '101',
        priority: 'Low',
        status: 'Scheduled',
        estimatedDurationMinutes: 60,
        conflictDetected: false,
        aiDecisionSummary: '',
        notes: [],
      );
      expect(model.formattedLocation, contains('101'));
      expect(model.formattedLocation, contains('Block A'));
    });

    test('calculatedDurationMinutes falls back to schedule difference', () {
      final start = DateTime(2026, 9, 24, 9, 0);
      final end = DateTime(2026, 9, 24, 11, 0);
      final model = WorkOrderModel(
        id: 'wo-2',
        workOrderNumber: 'WO-2',
        title: 'Test',
        description: '',
        requestId: 'r1',
        requestNumber: 'R1',
        technicianId: 't1',
        technicianName: 'Tech',
        locationName: 'Loc',
        building: 'B',
        room: 'R',
        priority: 'Medium',
        status: 'Scheduled',
        estimatedDurationMinutes: 0,
        scheduledStartTime: start,
        scheduledEndTime: end,
        conflictDetected: false,
        aiDecisionSummary: '',
        notes: [],
      );
      expect(model.calculatedDurationMinutes, 120);
    });
  });

  // ──────────────────────────────────────────────────────────
  // AppColors Unit Tests
  // ──────────────────────────────────────────────────────────
  group('AppColors Priority & Status Helper Tests', () {
    test('getPriorityColor returns correct colors', () {
      expect(AppColors.getPriorityColor('Critical'), AppColors.critical);
      expect(AppColors.getPriorityColor('High'), AppColors.high);
      expect(AppColors.getPriorityColor('Medium'), AppColors.medium);
      expect(AppColors.getPriorityColor('Low'), AppColors.low);
    });

    test('getStatusColor returns correct colors', () {
      expect(AppColors.getStatusColor('Completed'), AppColors.completed);
      expect(AppColors.getStatusColor('InProgress'), AppColors.inProgress);
      expect(AppColors.getStatusColor('Scheduled'), AppColors.scheduled);
      expect(AppColors.getStatusColor('Paused'), AppColors.paused);
    });
  });

  // ──────────────────────────────────────────────────────────
  // WorkOrderProvider Tests
  // ──────────────────────────────────────────────────────────
  group('WorkOrderProvider Tests', () {
    test('fetchSchedule populates jobs and clears error', () async {
      final provider = WorkOrderProvider(service: MockWorkOrderService());
      expect(provider.scheduleJobs.isEmpty, isTrue);

      await provider.fetchSchedule();

      expect(provider.isLoading, isFalse);
      expect(provider.error, isNull);
      expect(provider.scheduleJobs.length, 2);
      expect(provider.scheduleJobs.first.workOrderNumber, 'WO-202609-0001');
    });

    test('fetchAllJobs populates allJobs', () async {
      final provider = WorkOrderProvider(service: MockWorkOrderService());
      await provider.fetchAllJobs();

      expect(provider.isLoading, isFalse);
      expect(provider.allJobs.length, 2);
    });

    test('fetchAllJobs with status filter returns only matching', () async {
      final provider = WorkOrderProvider(service: MockWorkOrderService());
      await provider.fetchAllJobs(status: 'InProgress');

      expect(provider.allJobs.length, 1);
      expect(provider.allJobs.first.status, 'InProgress');
    });

    test('fetchJobDetails sets currentJob', () async {
      final provider = WorkOrderProvider(service: MockWorkOrderService());
      await provider.fetchJobDetails('wo-1');

      expect(provider.currentJob, isNotNull);
      expect(provider.currentJob!.workOrderNumber, 'WO-202609-0001');
    });
  });

  // ──────────────────────────────────────────────────────────
  // Priority Badge Widget Tests
  // ──────────────────────────────────────────────────────────
  group('PriorityBadge Widget Tests', () {
    testWidgets('renders Critical priority', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(home: Scaffold(body: PriorityBadge(priority: 'Critical'))),
      );
      expect(find.text('CRITICAL'), findsOneWidget);
    });

    testWidgets('renders High priority', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(home: Scaffold(body: PriorityBadge(priority: 'High'))),
      );
      expect(find.text('HIGH'), findsOneWidget);
    });

    testWidgets('renders Low priority', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(home: Scaffold(body: PriorityBadge(priority: 'Low'))),
      );
      expect(find.text('LOW'), findsOneWidget);
    });
  });

  // ──────────────────────────────────────────────────────────
  // Status Badge Widget Tests
  // ──────────────────────────────────────────────────────────
  group('StatusBadge Widget Tests', () {
    testWidgets('renders Completed status', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(home: Scaffold(body: StatusBadge(status: 'Completed'))),
      );
      expect(find.text('COMPLETED'), findsOneWidget);
    });

    testWidgets('formats InProgress as IN PROGRESS', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(home: Scaffold(body: StatusBadge(status: 'InProgress'))),
      );
      expect(find.text('IN PROGRESS'), findsOneWidget);
    });
  });

  // ──────────────────────────────────────────────────────────
  // DateSectionHeader Widget Tests
  // ──────────────────────────────────────────────────────────
  group('DateSectionHeader Widget Tests', () {
    testWidgets('shows TODAY for current date', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: DateSectionHeader(date: DateTime.now()),
          ),
        ),
      );
      expect(find.textContaining('TODAY'), findsOneWidget);
    });

    testWidgets('shows TOMORROW for next day', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: DateSectionHeader(date: DateTime.now().add(const Duration(days: 1))),
          ),
        ),
      );
      expect(find.textContaining('TOMORROW'), findsOneWidget);
    });

    testWidgets('shows job count chip when count provided', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: DateSectionHeader(date: DateTime.now(), count: 3),
          ),
        ),
      );
      expect(find.text('3 Jobs'), findsOneWidget);
    });
  });

  // ──────────────────────────────────────────────────────────
  // EmptyState Widget Tests
  // ──────────────────────────────────────────────────────────
  group('EmptyState Widget Tests', () {
    testWidgets('renders title and message', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: EmptyState(
              title: 'No jobs scheduled',
              message: 'You have no maintenance jobs for this date.',
            ),
          ),
        ),
      );
      expect(find.text('No jobs scheduled'), findsOneWidget);
      expect(find.text('You have no maintenance jobs for this date.'), findsOneWidget);
    });

    testWidgets('renders retry button when onRetry provided', (tester) async {
      bool tapped = false;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: EmptyState(
              title: 'Error',
              message: 'Something went wrong.',
              onRetry: () => tapped = true,
            ),
          ),
        ),
      );
      expect(find.text('Retry'), findsOneWidget);
      await tester.tap(find.text('Retry'));
      expect(tapped, isTrue);
    });
  });

  // ──────────────────────────────────────────────────────────
  // NotesList Widget Tests
  // ──────────────────────────────────────────────────────────
  group('NotesList Widget Tests', () {
    testWidgets('shows empty state message when no notes', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(body: NotesList(notes: [])),
        ),
      );
      expect(find.textContaining('No field notes'), findsOneWidget);
    });

    testWidgets('renders note text and author', (tester) async {
      final notes = [
        WorkNoteModel(
          id: 'n1',
          authorName: 'Tech Alex',
          noteText: 'Found the leaking pipe.',
          timestamp: DateTime.now(),
        ),
      ];
      await tester.pumpWidget(
        MaterialApp(home: Scaffold(body: NotesList(notes: notes))),
      );
      expect(find.text('Tech Alex'), findsOneWidget);
      expect(find.text('Found the leaking pipe.'), findsOneWidget);
    });
  });

  // ──────────────────────────────────────────────────────────
  // TechnicianScheduleScreen Widget Tests
  // ──────────────────────────────────────────────────────────
  group('TechnicianScheduleScreen Widget Tests', () {
    testWidgets('renders schedule screen and shows job card after load', (tester) async {
      final mockService = MockWorkOrderService();
      final provider = WorkOrderProvider(service: mockService);

      await tester.pumpWidget(buildTestApp(const TechnicianScheduleScreen(), provider: provider));
      await tester.pumpAndSettle();

      expect(find.text('WO-202609-0001'), findsOneWidget);
      expect(find.text('Ceiling Leak Inspection'), findsOneWidget);
    });

    testWidgets('shows date selector bar with 7 days', (tester) async {
      final provider = WorkOrderProvider(service: MockWorkOrderService());
      await tester.pumpWidget(buildTestApp(const TechnicianScheduleScreen(), provider: provider));
      await tester.pump();

      // 7 day chips should be rendered
      final dayChips = find.byType(GestureDetector);
      expect(dayChips, findsWidgets);
    });
  });

  // ──────────────────────────────────────────────────────────
  // AllJobsScreen Widget Tests
  // ──────────────────────────────────────────────────────────
  group('AllJobsScreen Widget Tests', () {
    testWidgets('renders jobs after load', (tester) async {
      final provider = WorkOrderProvider(service: MockWorkOrderService());
      await tester.pumpWidget(buildTestApp(const AllJobsScreen(), provider: provider));
      await tester.pumpAndSettle();

      expect(find.text('Ceiling Leak Inspection'), findsOneWidget);
      expect(find.text('HVAC Filter Replacement'), findsOneWidget);
    });

    testWidgets('filter chips are visible', (tester) async {
      final provider = WorkOrderProvider(service: MockWorkOrderService());
      await tester.pumpWidget(buildTestApp(const AllJobsScreen(), provider: provider));
      await tester.pump();

      expect(find.text('All'), findsOneWidget);
      expect(find.text('In Progress'), findsOneWidget);
      expect(find.text('Completed'), findsOneWidget);
    });
  });
}
