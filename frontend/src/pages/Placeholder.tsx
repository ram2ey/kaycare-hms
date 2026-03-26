interface Props {
  title: string;
}

export default function Placeholder({ title }: Props) {
  return (
    <div className="p-8">
      <h2 className="text-2xl font-semibold text-gray-800">{title}</h2>
      <p className="text-gray-500 mt-2">Coming soon.</p>
    </div>
  );
}
