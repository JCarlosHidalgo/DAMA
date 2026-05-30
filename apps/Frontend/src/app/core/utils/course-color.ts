export function courseColor(courseId: string): string {
  let hash = 0;
  for (let charIndex = 0; charIndex < courseId.length; charIndex++) {
    hash = (hash * 31 + courseId.charCodeAt(charIndex)) | 0;
  }
  const hue = Math.abs(hash) % 360;
  return `hsl(${hue}, 65%, 55%)`;
}
