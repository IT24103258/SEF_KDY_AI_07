import 'dart:async';
import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../core/routes/app_router.dart';
import '../core/theme/app_colors.dart';
import '../providers/work_order_provider.dart';
import '../widgets/add_note_modal.dart';
import '../widgets/job_timer.dart';
import '../widgets/notes_list.dart';
import '../widgets/photo_upload.dart';
import '../widgets/priority_badge.dart';
import '../widgets/status_badge.dart';

class JobExecutionScreen extends StatefulWidget {
  final String workOrderId;

  const JobExecutionScreen({super.key, required this.workOrderId});

  @override
  State<JobExecutionScreen> createState() => _JobExecutionScreenState();
}

class _JobExecutionScreenState extends State<JobExecutionScreen> {
  final GlobalKey<JobTimerState> _timerKey = GlobalKey<JobTimerState>();
  final List<PhotoItem> _photos = [];
  bool _isCompletingJob = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<WorkOrderProvider>().fetchJobDetails(widget.workOrderId);
    });
  }

  void _openAddNoteModal() {
    AddNoteModal.show(
      context,
      onSave: (text) async {
        final success = await context.read<WorkOrderProvider>().addNote(widget.workOrderId, text);
        return success;
      },
    );
  }

  Future<void> _handleCompleteJob() async {
    final timerState = _timerKey.currentState;
    timerState?.pauseTimer();

    Navigator.pushNamed(
      context,
      AppRouter.jobSignature,
      arguments: {
        'workOrderId': widget.workOrderId,
        'elapsedSeconds': timerState?.elapsedSeconds ?? 0,
        'photoCount': _photos.where((p) => p.isUploaded).length,
        'noteCount': context.read<WorkOrderProvider>().currentJob?.notes.length ?? 0,
        'uploadedFileKey': _photos.isEmpty ? null : _photos.firstWhere((p) => p.fileKey != null, orElse: () => PhotoItem(id: '', name: '', bytes: Uint8List(0))).fileKey,
      },
    );
  }

  Future<void> _handleUploadPhoto(PhotoItem item) async {
    final fileKey = await context.read<WorkOrderProvider>().uploadEvidencePhoto(
      widget.workOrderId,
      item.bytes,
      item.name,
    );
    if (fileKey != null) {
      setState(() {
        item.fileKey = fileKey;
        item.isUploaded = true;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<WorkOrderProvider>();
    final job = provider.currentJob;
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              job?.workOrderNumber ?? 'Executing Job',
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            if (job != null)
              Text(
                job.status,
                style: TextStyle(
                  fontSize: 11,
                  color: AppColors.getStatusColor(job.status),
                  fontWeight: FontWeight.w600,
                ),
              ),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh',
            onPressed: () => provider.fetchJobDetails(widget.workOrderId),
          ),
        ],
      ),
      body: provider.isLoading && job == null
          ? const Center(child: CircularProgressIndicator())
          : job == null
              ? const Center(child: Text('Work order not found.'))
              : ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    // Job Summary Card
                    Card(
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      child: Padding(
                        padding: const EdgeInsets.all(14),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                PriorityBadge(priority: job.priority),
                                const SizedBox(width: 8),
                                StatusBadge(status: job.status),
                              ],
                            ),
                            const SizedBox(height: 10),
                            Text(
                              job.title,
                              style: TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.bold,
                                color: isDark ? AppColors.darkTextPrimary : AppColors.lightTextPrimary,
                              ),
                            ),
                            const SizedBox(height: 6),
                            Row(
                              children: [
                                Icon(Icons.location_on_outlined, size: 14, color: isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted),
                                const SizedBox(width: 4),
                                Expanded(
                                  child: Text(
                                    job.formattedLocation,
                                    style: TextStyle(
                                      fontSize: 13,
                                      color: isDark ? AppColors.darkTextSecondary : AppColors.lightTextSecondary,
                                    ),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              ],
                            ),
                            if (job.scheduledStartTime != null) ...[
                              const SizedBox(height: 6),
                              Row(
                                children: [
                                  Icon(Icons.access_time_outlined, size: 14, color: isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted),
                                  const SizedBox(width: 4),
                                  Text(
                                    '${DateFormat('hh:mm a').format(job.scheduledStartTime!)} - ${job.scheduledEndTime != null ? DateFormat('hh:mm a').format(job.scheduledEndTime!) : '--:--'}',
                                    style: TextStyle(
                                      fontSize: 13,
                                      color: isDark ? AppColors.darkTextSecondary : AppColors.lightTextSecondary,
                                    ),
                                  ),
                                ],
                              ),
                            ],
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 14),

                    // Job Timer Widget
                    JobTimer(
                      key: _timerKey,
                      initialElapsedSeconds: 0,
                    ),
                    const SizedBox(height: 14),

                    // Field Work Notes Card
                    Card(
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Row(
                                  children: const [
                                    Icon(Icons.speaker_notes_outlined, color: AppColors.primary, size: 18),
                                    SizedBox(width: 8),
                                    Text(
                                      'Field Work Notes',
                                      style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                                    ),
                                  ],
                                ),
                                TextButton.icon(
                                  onPressed: _openAddNoteModal,
                                  icon: const Icon(Icons.add, size: 16),
                                  label: const Text('Add Note', style: TextStyle(fontSize: 12)),
                                ),
                              ],
                            ),
                            const Divider(height: 14),
                            NotesList(notes: job.notes),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 14),

                    // Completion Evidence: Photos
                    Card(
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: const [
                                Icon(Icons.photo_camera_outlined, color: AppColors.primary, size: 18),
                                SizedBox(width: 8),
                                Text(
                                  'Completion Evidence',
                                  style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                                ),
                              ],
                            ),
                            const SizedBox(height: 4),
                            Text(
                              'Take "after" photos showing completed repair.',
                              style: TextStyle(
                                fontSize: 12,
                                color: isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted,
                              ),
                            ),
                            const Divider(height: 18),
                            PhotoUpload(
                              photos: _photos,
                              onAddPhoto: (item) {
                                setState(() {
                                  _photos.add(item);
                                });
                              },
                              onRemovePhoto: (id) {
                                setState(() {
                                  _photos.removeWhere((p) => p.id == id);
                                });
                              },
                              onUploadPhoto: (item) async {
                                await _handleUploadPhoto(item);
                                return item.fileKey;
                              },
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 20),

                    // Complete Job Button
                    if (job.status.toLowerCase() != 'completed')
                      SizedBox(
                        height: 52,
                        child: ElevatedButton.icon(
                          onPressed: _isCompletingJob ? null : _handleCompleteJob,
                          icon: _isCompletingJob
                              ? const SizedBox(
                                  width: 18,
                                  height: 18,
                                  child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                                )
                              : const Icon(Icons.check_circle_outline, size: 22),
                          label: Text(
                            _isCompletingJob ? 'Navigating...' : 'Complete Job & Capture Signature',
                            style: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                          ),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppColors.completed,
                            foregroundColor: Colors.white,
                            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                          ),
                        ),
                      )
                    else
                      Container(
                        width: double.infinity,
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: AppColors.completedBg,
                          borderRadius: BorderRadius.circular(10),
                          border: Border.all(color: AppColors.completed.withOpacity(0.3)),
                        ),
                        child: const Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(Icons.check_circle, color: AppColors.completed, size: 24),
                            SizedBox(width: 10),
                            Text(
                              'Job Completed Successfully',
                              style: TextStyle(
                                fontWeight: FontWeight.bold,
                                color: AppColors.completed,
                                fontSize: 15,
                              ),
                            ),
                          ],
                        ),
                      ),
                    const SizedBox(height: 24),
                  ],
                ),
    );
  }
}
