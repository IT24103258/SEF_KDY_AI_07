import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../core/routes/app_router.dart';
import '../core/theme/app_colors.dart';
import '../models/work_order_model.dart';
import '../providers/work_order_provider.dart';
import '../widgets/app_top_bar.dart';
import '../widgets/date_section_header.dart';
import '../widgets/empty_state.dart';
import '../widgets/job_card.dart';

class TechnicianScheduleScreen extends StatefulWidget {
  const TechnicianScheduleScreen({super.key});

  @override
  State<TechnicianScheduleScreen> createState() =>
      _TechnicianScheduleScreenState();
}

class _TechnicianScheduleScreenState extends State<TechnicianScheduleScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<WorkOrderProvider>().fetchSchedule();
    });
  }

  Map<DateTime, List<WorkOrderModel>> _groupJobsByDate(
      List<WorkOrderModel> jobs) {
    final Map<DateTime, List<WorkOrderModel>> grouped = {};

    for (final job in jobs.where((job) => job.scheduledStartTime != null)) {
      final date = DateTime(
        job.scheduledStartTime!.year,
        job.scheduledStartTime!.month,
        job.scheduledStartTime!.day,
      );

      if (!grouped.containsKey(date)) {
        grouped[date] = [];
      }
      grouped[date]!.add(job);
    }

    // Sort jobs within each group chronologically
    for (final date in grouped.keys) {
      grouped[date]!.sort((a, b) {
        if (a.scheduledStartTime == null) return 1;
        if (b.scheduledStartTime == null) return -1;
        return a.scheduledStartTime!.compareTo(b.scheduledStartTime!);
      });
    }

    return grouped;
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<WorkOrderProvider>();
    final isDark = Theme.of(context).brightness == Brightness.dark;

    final groupedJobs = _groupJobsByDate(provider.scheduleJobs);
    final sortedDates = groupedJobs.keys.toList()
      ..sort((a, b) => a.compareTo(b));
    final unscheduledJobs = provider.scheduleJobs
        .where((job) => job.scheduledStartTime == null)
        .toList();

    return Scaffold(
      appBar: AppTopBar(
        title: 'My Schedule',
        onRefresh: provider.fetchSchedule,
      ),
      body: RefreshIndicator(
        onRefresh: provider.fetchSchedule,
        child: Column(
          children: [
            // Date Filter Bar
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              child: Row(
                children: [
                  Expanded(
                    child: InkWell(
                      borderRadius: BorderRadius.circular(8),
                      onTap: () async {
                        final picked = await showDatePicker(
                          context: context,
                          initialDate: provider.selectedDate,
                          firstDate: DateTime(2024),
                          lastDate: DateTime(2030),
                        );
                        if (picked != null) {
                          provider.setSelectedDate(picked);
                        }
                      },
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 12, vertical: 8),
                        decoration: BoxDecoration(
                          border: Border.all(
                              color: isDark
                                  ? AppColors.darkBorder
                                  : AppColors.lightBorder),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Row(
                          children: [
                            Icon(Icons.calendar_today_outlined,
                                size: 14,
                                color: isDark
                                    ? AppColors.darkTextMuted
                                    : AppColors.lightTextMuted),
                            const SizedBox(width: 8),
                            Text(
                              DateFormat('EEE, MMM d, yyyy')
                                  .format(provider.selectedDate),
                              style: TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w600,
                                color: isDark
                                    ? AppColors.darkTextPrimary
                                    : AppColors.lightTextPrimary,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  TextButton.icon(
                    onPressed: () {
                      provider.setSelectedDate(DateTime.now());
                    },
                    icon: const Icon(Icons.today, size: 16),
                    label: const Text('Today',
                        style: TextStyle(fontSize: 12)),
                  ),
                ],
              ),
            ),
            // Job List or States
            Expanded(
              child: provider.isLoading && provider.scheduleJobs.isEmpty
                  ? const Center(child: CircularProgressIndicator())
                  : provider.error != null
                      ? EmptyState(
                          icon: Icons.error_outline,
                          title: 'Unable to Load Schedule',
                          message: provider.error!,
                          retryButtonText: 'Retry Schedule',
                          onRetry: provider.fetchSchedule,
                        )
                      : provider.scheduleJobs.isEmpty
                          ? EmptyState(
                              icon: Icons.event_available_outlined,
                              title: 'No jobs scheduled',
                              message:
                                  'Your upcoming assigned maintenance jobs will appear here.',
                              retryButtonText: 'Refresh Schedule',
                              onRetry: provider.fetchSchedule,
                            )
                          : ListView.builder(
                              padding: const EdgeInsets.symmetric(
                                  horizontal: 16, vertical: 8),
                              itemCount: sortedDates.length +
                                  (unscheduledJobs.isEmpty ? 0 : 1),
                              itemBuilder: (context, dateIndex) {
                                final isUnscheduledSection =
                                    dateIndex == sortedDates.length;
                                final dateJobs = isUnscheduledSection
                                    ? unscheduledJobs
                                    : groupedJobs[sortedDates[dateIndex]] ?? [];

                                return Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    if (isUnscheduledSection)
                                      const Padding(
                                        padding:
                                            EdgeInsets.fromLTRB(4, 16, 4, 10),
                                        child: Text(
                                          'UNSCHEDULED',
                                          style: TextStyle(
                                            fontSize: 12,
                                            fontWeight: FontWeight.bold,
                                            letterSpacing: 0.8,
                                          ),
                                        ),
                                      )
                                    else
                                      DateSectionHeader(
                                        date: sortedDates[dateIndex],
                                        count: dateJobs.length,
                                      ),
                                    ...dateJobs.map((job) {
                                      return JobCard(
                                        job: job,
                                        onDetails: () {
                                          Navigator.pushNamed(
                                            context,
                                            AppRouter.jobDetailsPath(job.id),
                                          );
                                        },
                                        onStart: () async {
                                          if (job.status.toLowerCase() ==
                                              'inprogress') {
                                            Navigator.pushNamed(
                                              context,
                                              AppRouter.jobExecutionPath(
                                                  job.id),
                                            );
                                          } else {
                                            final success =
                                                await provider.startJob(job.id);
                                            if (success && context.mounted) {
                                              Navigator.pushNamed(
                                                context,
                                                AppRouter.jobExecutionPath(
                                                    job.id),
                                              );
                                            }
                                          }
                                        },
                                      );
                                    }),
                                  ],
                                );
                              },
                            ),
            ),
          ],
        ),
      ),
    );
  }
}
