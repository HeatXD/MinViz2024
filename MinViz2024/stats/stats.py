import pandas as pd
import numpy as np
import matplotlib.pyplot as plt

def analyze_aco_convergence(df):
    """
    Analyzes when ACO outperforms NNH based on iterations and elapsed time.
    
    Parameters:
    df: pandas DataFrame with columns [Algo, Distance, Seed, ElapsedTime, PointCount, 
                                     CubicVolume, Iterations, AOSPositions]
    """
    
    def calculate_convergence_stats(group):
        """Calculate when ACO beats NNH for a specific problem configuration"""
        # Get the NNH solution for this configuration
        nnh_data = group[group['Algo'] == 'NNH']
        if len(nnh_data) == 0:
            return None
        
        nnh_distance = nnh_data['Distance'].mean()
        nnh_time_ms = (nnh_data['ElapsedTime'].mean() * 100) / 1_000_000  # Convert ticks to ms
        
        # Get ACO solutions ordered by iteration
        aco_data = group[group['Algo'] == 'ACO'].sort_values('Iterations')
        if len(aco_data) == 0:
            return None
            
        # Find first iteration where ACO beats NNH
        beating_solutions = aco_data[aco_data['Distance'] < nnh_distance]
        if len(beating_solutions) == 0:
            return {
                'beats_nnh': False,
                'convergence_iteration': np.nan,
                'convergence_time_ms': np.nan,
                'final_improvement': 0,
                'nnh_distance': nnh_distance,
                'nnh_time_ms': nnh_time_ms,
                'best_aco_distance': aco_data['Distance'].min(),
                'best_aco_time_ms': (aco_data['ElapsedTime'].min() * 100) / 1_000_000,
                'point_count': group['PointCount'].iloc[0]
            }
            
        first_better = beating_solutions.iloc[0]
        
        return {
            'beats_nnh': True,
            'convergence_iteration': first_better['Iterations'],
            'convergence_time_ms': (first_better['ElapsedTime'] * 100) / 1_000_000,
            'final_improvement': ((nnh_distance - beating_solutions['Distance'].min()) / nnh_distance) * 100,
            'nnh_distance': nnh_distance,
            'nnh_time_ms': nnh_time_ms,
            'best_aco_distance': beating_solutions['Distance'].min(),
            'best_aco_time_ms': (beating_solutions['ElapsedTime'].min() * 100) / 1_000_000,
            'point_count': group['PointCount'].iloc[0]
        }

    # Group by unique problem configurations
    grouped = df.groupby(['PointCount', 'CubicVolume', 'Seed'])
    convergence_data = []
    
    for _, group in grouped:
        stats = calculate_convergence_stats(group)
        if stats is not None:
            convergence_data.append(stats)
    
    results_df = pd.DataFrame(convergence_data)
    
    # Calculate summary statistics
    summary = {
        'total_configurations': len(results_df),
        'configurations_aco_wins': len(results_df[results_df['beats_nnh']]),
        'avg_convergence_iteration': results_df['convergence_iteration'].mean(),
        'median_convergence_iteration': results_df['convergence_iteration'].median(),
        'avg_convergence_time_ms': results_df['convergence_time_ms'].mean(),
        'median_convergence_time_ms': results_df['convergence_time_ms'].median(),
        'avg_improvement_percentage': results_df['final_improvement'].mean(),
        'success_rate': (len(results_df[results_df['beats_nnh']]) / len(results_df)) * 100
    }
    
    # Create visualization
    fig, ((ax1, ax2), (ax3, ax4)) = plt.subplots(2, 2, figsize=(15, 12))
    
    # Plot 1: Convergence iterations by point count
    successful_cases = results_df[results_df['beats_nnh']]
    ax1.scatter(successful_cases['point_count'], 
               successful_cases['convergence_iteration'],
               alpha=0.6)
    ax1.set_xlabel('Point Count')
    ax1.set_ylabel('Iterations until ACO beats NNH')
    ax1.set_title('Convergence Speed by Problem Size (Iterations)')
    
    # Plot 2: Improvement percentage by point count
    ax2.scatter(results_df['point_count'], 
               results_df['final_improvement'],
               alpha=0.6)
    ax2.set_xlabel('Point Count')
    ax2.set_ylabel('Improvement over NNH (%)')
    ax2.set_title('Solution Quality Improvement')
    
    # Plot 3: Convergence time by point count
    ax3.scatter(successful_cases['point_count'],
               successful_cases['convergence_time_ms'],
               alpha=0.6)
    ax3.set_xlabel('Point Count')
    ax3.set_ylabel('Time until ACO beats NNH (ms)')
    ax3.set_title('Convergence Speed by Problem Size (Time)')
    
    # Plot 4: Time comparison ACO vs NNH
    ax4.scatter(results_df['point_count'],
               results_df['best_aco_time_ms'] / results_df['nnh_time_ms'],
               alpha=0.6)
    ax4.axhline(y=1, color='r', linestyle='--', alpha=0.5)
    ax4.set_xlabel('Point Count')
    ax4.set_ylabel('ACO/NNH Time Ratio')
    ax4.set_title('Computational Efficiency Comparison')
    
    plt.tight_layout()
    
    # Detailed statistics by point count
    point_count_stats = results_df.groupby('point_count').agg({
        'convergence_iteration': ['mean', 'median', 'std'],
        'convergence_time_ms': ['mean', 'median', 'std'],
        'final_improvement': ['mean', 'std'],
        'beats_nnh': 'mean'  # This gives us the success rate
    }).round(2)
    
    return {
        'summary': summary,
        'detailed_stats': point_count_stats,
        'raw_results': results_df,
        'visualization': fig
    }

def print_convergence_summary(results):
    """Print a formatted summary of the convergence analysis"""
    summary = results['summary']
    print("\n=== ACO vs NNH Convergence Analysis ===")
    print(f"\nTotal problem configurations analyzed: {summary['total_configurations']}")
    print(f"Configurations where ACO beats NNH: {summary['configurations_aco_wins']}")
    print(f"Overall success rate: {summary['success_rate']:.2f}%")
    print(f"\nWhen ACO beats NNH:")
    print(f"- Average iterations until better solution: {summary['avg_convergence_iteration']:.2f}")
    print(f"- Median iterations until better solution: {summary['median_convergence_iteration']:.2f}")
    print(f"- Average time until better solution: {summary['avg_convergence_time_ms']:.2f} ms")
    print(f"- Median time until better solution: {summary['median_convergence_time_ms']:.2f} ms")
    print(f"- Average improvement over NNH: {summary['avg_improvement_percentage']:.2f}%")
    
    print("\n=== Statistics by Point Count ===")
    print(results['detailed_stats'])

df = pd.read_csv('../bin/Release/net8.0/BenchResults.csv')
results = analyze_aco_convergence(df)
print_convergence_summary(results)
plt.show()