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
  State<TechnicianScheduleScreen> createState() => _TechnicianScheduleScreenState();
}

class _TechnicianScheduleScreenState extends State<TechnicianScheduleScreen> {
  DateTime _selectedDate = DateTime.now();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<WorkOrderProvider>().fetchSchedule(filterDate: _selectedDate);
    });
  }

  void _onSelectDate(DateTime date) {
    setState(() {
      _selectedDate = date;
    });
    context.read<WorkOrderProvider>().setSelectedDate(date);
  }

  Map<DateTime, List<WorkOrderModel>> _groupJobsByDate(List<WorkOrderModel> jobs) {
    final Map<DateTime, List<WorkOrderModel>> grouped = {};

    for (final job in jobs) {
      final date = job.scheduledStartTime != null
          ? DateTime(job.scheduledStartTime!.year, job.scheduledStartTime!.month, job.scheduledStartTime!.day)
          : DateTime(_selectedDate.year, _selectedDate.month, _selectedDate.day);

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
    final sortedDates = groupedJobs.keys.toList()..sort((a, b) => a.compareTo(b));

    return Scaffold(
      appBar: AppTopBar(
        title: 'My Schedule',
        onRefresh: () => provider.fetchSchedule(filterDate: _selectedDate),
      ),
      body: RefreshIndicator(
        onRefresh: () => provider.fetchSchedule(filterDate: _selectedDate),
        child: Column(
          children: [
            // Horizontal Day Selector Bar
            Container(
              padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 8),
              decoration: BoxDecoration(
                color: isDark ? AppColors.darkCard : Colors.white,
                border: Border(
                  bottom: BorderSide(
                    color: isDark ? AppColors.darkBorder : AppColors.lightBorder,
                    width: 1,
                  ),
                ),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceAround,
                children: List.generate(7, (index) {
                  final date = DateTime.now().add(Duration(days: index - 2));
                  final isSelected = date.day == _selectedDate.day &&
                      date.month == _selectedDate.month &&
                      date.year == _selectedDate.year;

                  return GestureDetector(
                    onTap: () => _onSelectDate(date),
                    child: Container(
                      padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 10),
                      decoration: BoxDecoration(
                        color: isSelected ? AppColors.primary : Colors.transparent,
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Column(
                        children: [
                          Text(
                            DateFormat('E').format(date).toUpperCase(),
                            style: TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w700,
                              color: isSelected
                                  ? Colors.white
                                  : (isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted),
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            DateFormat('d').format(date),
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                              color: isSelected
                                  ? Colors.white
                                  : (isDark ? AppColors.darkTextPrimary : AppColors.lightTextPrimary),
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                }),
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
                          onRetry: () => provider.fetchSchedule(filterDate: _selectedDate),
                        )
                      : provider.scheduleJobs.isEmpty
                          ? EmptyState(
                              icon: Icons.event_available_outlined,
                              title: 'No jobs scheduled',
                              message: 'You have no maintenance jobs scheduled for ${DateFormat('EEEE, MMM d').format(_selectedDate)}.',
                              retryButtonText: 'Refresh Schedule',
                              onRetry: () => provider.fetchSchedule(filterDate: _selectedDate),
                            )
                          : ListView.builder(
                              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                              itemCount: sortedDates.length,
                              itemBuilder: (context, dateIndex) {
                                final date = sortedDates[dateIndex];
                                final dateJobs = groupedJobs[date] ?? [];

                                return Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    DateSectionHeader(
                                      date: date,
                                      count: dateJobs.length,
                                    ),
                                    ...dateJobs.map((job) {
                                      return JobCard(
                                        job: job,
                                        onDetails: () {
                                          Navigator.pushNamed(
                                            context,
                                            AppRouter.jobDetails,
                                            arguments: job.id,
                                          );
                                        },
                                        onStart: () async {
                                          if (job.status.toLowerCase() == 'inprogress') {
                                            Navigator.pushNamed(
                                              context,
                                              AppRouter.jobExecution,
                                              arguments: job.id,
                                            );
                                          } else {
                                            final success = await provider.startJob(job.id);
                                            if (success && context.mounted) {
                                              Navigator.pushNamed(
                                                context,
                                                AppRouter.jobExecution,
                                                arguments: job.id,
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
